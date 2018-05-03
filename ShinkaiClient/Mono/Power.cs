using UnityEngine;
using ShinkaiModel.Core;
using ShinkaiModel.Networking;
using ShinkaiClient.Mono;
using ShinkaiClient.Unity;

namespace ShinkaiClient.Mono {
	public abstract class PowerMonitor : MonoBehaviour {
		protected RateLimiter rateLimiter;
		protected float lastCharge;

		protected abstract float GetCharge();
		protected abstract void SetCharge(float value, float target);

		public virtual void Awake() {
			rateLimiter = new RateLimiter();
			lastCharge = GetCharge();
		}

		public void Update() {
			if (rateLimiter.Update(8.0f)) {
				float charge = GetCharge();
				if (Mathf.Abs(lastCharge - charge) >= 5.0f) {
					lastCharge = charge;

					if (GameModeUtils.RequiresPower()) {
						var res = new ClientPowerChange();
						res.targetGuid = GuidHelper.Get(gameObject);
						res.total = charge;
						res.force = false;
						Multiplayer.main.Send(res);
					}
				}
			}
		}

		public void Correct(float newCharge) {
			float charge = GetCharge();
			if (charge > newCharge) {
				SetCharge(charge, newCharge);
				lastCharge = newCharge;
				rateLimiter.Reset();
			}
		}

		public void Force(float newCharge) {
			SetCharge(GetCharge(), newCharge);
			lastCharge = newCharge;
			rateLimiter.Reset();
		}
	}

	public class EnergyInterfaceMonitor : PowerMonitor {
		private EnergyInterface target;

		public override void Awake() {
			target = GetComponent<EnergyInterface>();
			base.Awake();
		}

		protected override float GetCharge() {
			float total = 0.0f;
			foreach (var source in target.sources) {
				if (source != null) {
					total += source.charge;
				}
			}

			return total;
		}

		protected override void SetCharge(float oldCharge, float newCharge) {
			if (oldCharge > newCharge) {
				this.target.ConsumeEnergy(oldCharge - newCharge);
			} else if (newCharge > oldCharge) {
				this.target.AddEnergy(newCharge - oldCharge);
			}
		}
	}

	public class PowerRelayMonitor : PowerMonitor {
		private PowerRelay target;
		private bool connected;

		public override void Awake() {
			target = GetComponentInChildren<PowerRelay>();
			base.Awake();
		}

		protected override float GetCharge() {
			return target.GetPower();
		}

		protected override void SetCharge(float oldCharge, float newCharge) {
			if (connected == false) {
				connected = true;
				target.UpdateConnection();
			}

			float modified = 0f;
			float amount = newCharge - oldCharge;

			if (this.target.internalPowerSource) {
				this.target.internalPowerSource.ModifyPower(amount, out modified);
			} else {
				this.target.ModifyPowerFromInbound(amount, out modified);
			}
		}
	}
}
