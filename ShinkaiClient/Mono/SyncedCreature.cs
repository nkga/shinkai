using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using ShinkaiModel.Core;
using ShinkaiModel.Networking;
using ShinkaiClient.Unity;

namespace ShinkaiClient.Mono {
	public class SyncedCreature : MonoBehaviour {
		static FieldInfo prevActionField = typeof(Creature).GetField("prevBestAction", BindingFlags.NonPublic | BindingFlags.Instance);
		static FieldInfo nextUpdateField = typeof(Creature).GetField("nextUpdateTime", BindingFlags.NonPublic | BindingFlags.Instance);

		public const float maxDestroyDistSq = 50.0f * 50.0f;
		public const float maxSpawnDistSq = 30.0f * 30.0f;
		public const float maxValidDistSq = 15.0f * 15.0f;

		public bool ownership;
		protected RateLimiter rateLimiter;
		protected Creature creature;
		protected ClientCreatureUpdate res;
		protected List<CreatureAction> actions;

		public void Awake() {
			rateLimiter = new RateLimiter();
			creature = gameObject.GetComponent<Creature>();
			if (creature != null) {
				actions = (List<CreatureAction>)typeof(Creature).GetField("actions", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(creature);
			}

			res = new ClientCreatureUpdate();
			res.creatureGuid = GuidHelper.Get(gameObject);
			res.tech = CraftData.GetTechType(gameObject);
		}

		public void Update() {
			if (ownership == false) {
				if (Vector3.SqrMagnitude(Player.main.transform.position - transform.position) > maxDestroyDistSq) {
					GameObject.Destroy(gameObject);
				}
			} else {

				if (rateLimiter.Update(10.0f)) {
					res.timestamp = DayNightCycle.main.timePassedAsDouble;
					res.position = transform.position;
					res.rotation = transform.rotation;

					if (creature != null) {
						res.leashPosition = creature.leashPosition;

						if (actions != null) {
							var action = creature.GetBestAction();
							res.actionIndex = actions.IndexOf(action);
						}
					}

					Multiplayer.main.Send(res, LiteNetLib.DeliveryMethod.Sequenced);
				}
			}
		}

		public void Correct(ClientCreatureUpdate msg) {
			if (Vector3.SqrMagnitude(transform.position - msg.position) > maxValidDistSq) {
				transform.position = msg.position;
				transform.rotation = msg.rotation;
			}

			if (creature != null) {
				creature.leashPosition = msg.leashPosition;
			}

			if (actions != null && msg.actionIndex >= 0 && msg.actionIndex < actions.Count) {
				var oldAction = creature.GetBestAction();
				var newAction = actions[msg.actionIndex];

				if (newAction != oldAction) {
					if (oldAction != null) {
						oldAction.StopPerform(creature);
					}

					if (newAction != null) {
						newAction.StartPerform(creature);
					}

					prevActionField.SetValue(creature, newAction);
				}

				if (newAction != null) {
					newAction.Perform(creature, Time.deltaTime);
				}
			}
		}
	}
}
