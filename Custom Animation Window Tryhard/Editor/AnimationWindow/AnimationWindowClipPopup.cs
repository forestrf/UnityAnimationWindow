// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditorInternal.Enemeteen;
using System.Linq;

namespace UnityEditor.Enemeteen {
	[System.Serializable]
	class AnimationWindowClipPopup {
		[SerializeField] public AnimationWindowState state;

		ClipPopupCallbackInfo popupCI = new ClipPopupCallbackInfo();
		ClipPopupCallbackInfo addCI = new ClipPopupCallbackInfo();

		internal sealed class ClipPopupCallbackInfo {
			// Name of the command event sent from the popup menu to OnGUI when user has changed selection
			private const string kPopupMenuChangedMessage = "ClipPopupMenuChanged";

			// Which item was selected
			public AnimationClip selectedClip;

			// Which view should we send it to.
			private GUIView m_SourceView;

			public void SetSourceView() {
				m_SourceView = GUIView.current;
			}

			public AnimationClip GetSelectedClipForControl(AnimationClip clip) {
				Event evt = Event.current;
				if (evt.type == EventType.ExecuteCommand && evt.commandName == kPopupMenuChangedMessage) {
					clip = selectedClip;
					GUI.changed = true;
					evt.Use();
				}
				return clip;
			}

			public void SendEvent() {
				m_SourceView.SendEvent(EditorGUIUtility.CommandEvent(kPopupMenuChangedMessage));
			}
		}


		private void DisplayClipMenu(Rect position, AnimationClip clip) {
			popupCI.SetSourceView();

			AnimationSelectorWindow.DrawPresetButton(clip, GetOrderedClipList(), position, (selectedClip) => {
				popupCI.selectedClip = selectedClip;
				popupCI.SendEvent();
			});
		}

		// (case 1029160) Modified version of EditorGUI.DoPopup to fit large data list query.
		private AnimationClip DoClipPopup(AnimationClip clip, GUIStyle style) {
			Rect position = EditorGUILayout.GetControlRect(false, EditorGUI.kSingleLineHeight, style);

			clip = popupCI.GetSelectedClipForControl(clip);
			clip = addCI.GetSelectedClipForControl(clip);

			Font originalFont = style.font;
			if (originalFont && EditorGUIUtility.GetBoldDefaultFont() && originalFont == EditorStyles.miniFont) {
				style.font = EditorStyles.miniBoldFont;
			}

			GUIContent buttonContent = EditorGUIUtility.TempContent(CurveUtility.GetClipName(clip));
			buttonContent.tooltip = AssetDatabase.GetAssetPath(clip);

			if (GUI.Button(position, buttonContent, style)) {
				DisplayClipMenu(position, clip);
			}

			return clip;
		}

		public void OnGUI() {
			if (state.selection.canChangeAnimationClip) {
				var newClip = DoClipPopup(state.activeAnimationClip, AnimationWindowStyles.animClipToolbarPopup);
				if (state.selection.canCreateClips) {
					addCI.SetSourceView();
					if (GUILayout.Button(new GUIContent("+", "Create New Clip..."), EditorStyles.toolbarButton)) {
						newClip = AnimationWindowUtility.CreateNewClip(state.selection.rootGameObject.name);
						if (newClip) {
							AnimationWindowUtility.AddClipToAnimationPlayerComponent(state.activeAnimationPlayer, newClip);
							addCI.selectedClip = newClip;
							addCI.SendEvent();
						}
					}
				}
				if (state.activeAnimationClip != newClip) {
					state.activeAnimationClip = newClip;

					//  Layout has changed, bail out now.
					EditorGUIUtility.ExitGUI();
				}
			}
			else if (state.activeAnimationClip != null) {
				Rect r = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight, AnimationWindowStyles.toolbarLabel);
				EditorGUI.LabelField(r, CurveUtility.GetClipName(state.activeAnimationClip), AnimationWindowStyles.toolbarLabel);
			}
		}

		private AnimationClip[] GetOrderedClipList() {
			AnimationClip[] clips = new AnimationClip[0];
			if (state.activeRootGameObject != null)
				clips = AnimationUtility.GetAnimationClips(state.activeRootGameObject);

			clips = clips.Distinct().ToArray();

			Array.Sort(clips, (AnimationClip clip1, AnimationClip clip2) => CurveUtility.GetClipName(clip1).CompareTo(CurveUtility.GetClipName(clip2)));

			return clips;
		}
	}
}
