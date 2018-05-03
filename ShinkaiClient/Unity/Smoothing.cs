using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ShinkaiClient.Unity {
	public class SmoothFloat {
		public const float speed = 10f;
		public float target;
		public float value;

		public SmoothFloat() {
			target = value = 0f;
		}

		public SmoothFloat(float initial) {
			target = value = initial;
		}

		public void Update() {
			float delta = target - value;
			if (Mathf.Abs(delta) > 1.0e-4f) {
				value = UWE.Utils.Slerp(value, target, delta * speed * Time.deltaTime);
			} else {
				target = value;
			}
		}
	}

	public class SmoothVector {
		public const float speed = 10f;
		public Vector3 target;
		public Vector3 value;

		public SmoothVector() {
		}

		public SmoothVector(Vector3 initial) {
			target = value = initial;
		}

		public void Update() {
			value = UWE.Utils.SlerpVector(value, target, (target - value) * speed * Time.deltaTime);
		}
	}

	public class SmoothQuaternion {
		public const float speed = 10f;
		public Quaternion target;
		public Quaternion value;

		public SmoothQuaternion() {
		}

		public SmoothQuaternion(Quaternion initial) {
			target = value = initial;
		}

		public void Update() {
			value = Quaternion.Slerp(value, target, speed * Time.fixedDeltaTime);
		}
	}

	public class SmoothTransform : MonoBehaviour {
		private const float maxSmoothNetUpdateDist = 6f;
		private const float noSmoothNetUpdateDist = 8f;
		private const float maxMoveDeltaTime = 0.120f;
		private const float maxClientTimeAheadPercent = 0.10f;

		private Rigidbody rigidbody;
		private Vector3 realVelocity;
		private Vector3 realLocation;
		private Quaternion realRotation;

		private Vector3 meshTranslationOffset;
		private Vector3 originalMeshTranslationOffset;
		private Quaternion originalMeshRotationOffset;
		private Quaternion meshRotationOffset;
		private Quaternion meshRotationTarget;

		private double clientTimeStamp;
		private double serverTimestamp;
		private float lastCorrectionDelta;
		private bool lastCorrectionSet;
		private bool smoothingEnabled;

		public bool smoothingComplete;

		private void Awake() {
			rigidbody = gameObject.GetComponent<Rigidbody>();
			SetDisabled();
		}

		private void Update() {
			clientTimeStamp += Time.deltaTime;

			if (smoothingEnabled) {
				Interpolate();
			}
		}

		public void Correct(Vector3 newLocation, Quaternion newRotation, Vector3 newVelocity, double newTimestamp) {
			Vector3 oldLocation = transform.position;
			Quaternion oldRotation = transform.rotation;

			Vector3 newToOldVector = (oldLocation - newLocation);
			float distSq = newToOldVector.sqrMagnitude;

			if (distSq > maxSmoothNetUpdateDist * maxSmoothNetUpdateDist) {
				if (distSq > noSmoothNetUpdateDist * noSmoothNetUpdateDist) {
					meshTranslationOffset = Vector3.zero;
				} else {
					meshTranslationOffset += newToOldVector.normalized * maxSmoothNetUpdateDist;
				}
			} else {
				meshTranslationOffset += newToOldVector;
			}

			originalMeshTranslationOffset = meshTranslationOffset;
			originalMeshRotationOffset = oldRotation;
			meshRotationOffset = oldRotation;
			meshRotationTarget = newRotation;

			// If running ahead, pull back slightly. This will cause the next delta to seem slightly longer, and cause us to lerp to it slightly slower.
			if (clientTimeStamp > serverTimestamp) {
				double oldClientTimestamp = clientTimeStamp;
				clientTimeStamp = TimeLerp(serverTimestamp, clientTimeStamp, 0.5);
			}

			// Using server timestamp lets us know how much time actually elapsed, regardless of packet lag variance.
			double oldServerTimestamp = serverTimestamp;
			serverTimestamp = newTimestamp;

			// Initial update has no delta.
			if (lastCorrectionSet == false) {
				lastCorrectionSet = true;
				clientTimeStamp = serverTimestamp;
				oldServerTimestamp = serverTimestamp;
			}

			// Don't let the client fall too far behind or run ahead of new server time.
			double serverDeltaTime = serverTimestamp - oldServerTimestamp;
			double maxDelta = TimeClamp(serverDeltaTime * 1.25, 0.0, maxMoveDeltaTime * 2.0f);
			clientTimeStamp = TimeClamp(clientTimeStamp, serverTimestamp - maxDelta, serverTimestamp);

			lastCorrectionDelta = (float)(serverTimestamp - clientTimeStamp);

			// Store actual position / rotation values.
			realVelocity = newVelocity;
			realLocation = newLocation;
			realRotation = newRotation;
		}

		private void Interpolate() {
			const float lerpLimit = 1.15f;

			float lerpPercent = 0f;
			float targetDelta = lastCorrectionDelta;

			if (targetDelta > Mathf.Epsilon) {
				// Don't let the client get too far ahead (happens on spikes). But we do want a buffer for variable network conditions.
				float maxTimeAhead = targetDelta * maxClientTimeAheadPercent;
				clientTimeStamp = TimeMin(clientTimeStamp, serverTimestamp + maxTimeAhead);

				// Compute interpolation alpha based on our client position within the server delta. We should take TargetDelta seconds to reach alpha of 1.
				float remainingTime = (float)(serverTimestamp - clientTimeStamp);
				float smoothTime = targetDelta - remainingTime;
				lerpPercent = Mathf.Clamp(smoothTime / targetDelta, 0.0f, lerpLimit);
			} else {
				lerpPercent = 1.0f;
			}

			if (lerpPercent >= 1.0f - Mathf.Epsilon) {
				if (NearlyZero(realVelocity)) {
					meshTranslationOffset = Vector3.zero;
					clientTimeStamp = serverTimestamp;
					smoothingComplete = true;
				} else {
					// Allow limited forward prediction.
					meshTranslationOffset = LerpStable(originalMeshTranslationOffset, Vector3.zero, lerpPercent);
					smoothingComplete = (lerpPercent >= lerpLimit);
				}

				meshRotationOffset = meshRotationTarget;
			} else {
				meshTranslationOffset = LerpStable(meshTranslationOffset, Vector3.zero, lerpPercent);
				meshRotationOffset = Quaternion.LerpUnclamped(meshRotationOffset, meshRotationTarget, lerpPercent);
			}


			if (rigidbody != null) {
				rigidbody.MovePosition(realLocation + meshTranslationOffset);
				rigidbody.MoveRotation(meshRotationOffset);
			} else {
				transform.position = realLocation + meshTranslationOffset;
				transform.rotation = meshRotationOffset;
			}
		}

		public void SetDisabled() {
			if (smoothingEnabled != false) {
				transform.position = realLocation;
				transform.rotation = realRotation;
				smoothingComplete = true;
				smoothingEnabled = false;

				if (rigidbody != null) {
					rigidbody.interpolation = RigidbodyInterpolation.None;
				}
			}
		}

		public void SetEnabled() {
			if (smoothingEnabled == false) {
				lastCorrectionSet = false;
				smoothingComplete = false;
				smoothingEnabled = true;

				if (rigidbody != null) {
					rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
				}
			}
		}

		private bool NearlyZero(Vector3 vector, float tolerance = 1.0e-4f) {
			return Mathf.Abs(vector.x) <= tolerance && Mathf.Abs(vector.y) <= tolerance && Mathf.Abs(vector.z) <= tolerance;
		}

		private double TimeClamp(double x, double min, double max) {
			return x < min ? min : x < max ? x : max;
		}

		private double TimeLerp(double a, double b, double alpha) {
			return ((a * (1.0 - alpha)) + (b * alpha));
		}

		private double TimeMin(double a, double b) {
			return (a <= b) ? a : b;
		}

		private Vector3 LerpStable(Vector3 a, Vector3 b, float alpha) {
			return (a * (1.0f - alpha) + (b * alpha));
		}
	}
}
