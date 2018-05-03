using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ShinkaiClient.Unity {
	public static class AnimatorExtensions {
		public static void ResetParameters(this Animator animator) {
			AnimatorControllerParameter[] parameters = animator.parameters;
			for (int i = 0; i < parameters.Length; i++) {
				AnimatorControllerParameter parameter = parameters[i];
				switch (parameter.type) {
					case AnimatorControllerParameterType.Int:
						animator.SetInteger(parameter.name, parameter.defaultInt);
						break;
					case AnimatorControllerParameterType.Float:
						animator.SetFloat(parameter.name, parameter.defaultFloat);
						break;
					case AnimatorControllerParameterType.Bool:
						animator.SetBool(parameter.name, parameter.defaultBool);
						break;
				}
			}
		}
	}
}
