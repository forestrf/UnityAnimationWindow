using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
///   <para>This class implements a modal window that selects an Animation asset from the Project. Modification of the Preset modal window selector.</para>
/// </summary>
public class AnimationSelectorWindow : EditorWindow {
	private static class Style {
		public static GUIStyle bottomBarBg = "ProjectBrowserBottomBarBg";
		public static GUIStyle toolbarBack = "ObjectPickerToolbar";
	}

	private string m_SearchField = "";
	private ObjectListAreaState m_ListAreaState;
	private ObjectListArea m_ListArea;
	private SavedInt m_StartGridSize = new SavedInt("AnimationSelector.GridSize", 64);
	private int m_ModalUndoGroup = -1;
	private Action<AnimationClip> onSelect;
	public AnimationClip[] clips;

	public static void DrawPresetButton(AnimationClip current, AnimationClip[] clips, Rect position, Action<AnimationClip> onSelect) {
		AnimationSelectorWindow window = CreateInstance<AnimationSelectorWindow>();
		window.clips = clips;
		window.position = position;
		window.Init(current, onSelect);
		window.ShowPopup();
	}

	private void Init(AnimationClip current, Action<AnimationClip> onSelect) {
		m_ModalUndoGroup = Undo.GetCurrentGroup();
		ContainerWindow.SetFreezeDisplay(freeze: true);
		InitListArea();
		UpdateSearchResult((current != null) ? current.GetInstanceID() : 0);
		this.onSelect = onSelect;
		ShowWithMode(ShowMode.AuxWindow);
		base.titleContent = EditorGUIUtility.TrTextContent("Select Animation");
		Rect position = m_Parent.window.position;
		base.position = position;
		base.minSize = new Vector2(200f, 335f);
		base.maxSize = new Vector2(10000f, 10000f);
		Focus();
		ContainerWindow.SetFreezeDisplay(freeze: false);
		m_Parent.AddToAuxWindowList();
	}

	private void InitListArea() {
		if (m_ListAreaState == null) {
			m_ListAreaState = new ObjectListAreaState();
		}
		if (m_ListArea == null) {
			m_ListArea = new ObjectListArea(m_ListAreaState, this, showNoneItem: true);
			m_ListArea.allowDeselection = false;
			m_ListArea.allowDragging = false;
			m_ListArea.allowFocusRendering = false;
			m_ListArea.allowMultiSelect = false;
			m_ListArea.allowRenaming = false;
			m_ListArea.allowBuiltinResources = false;
			ObjectListArea listArea = m_ListArea;
			listArea.repaintCallback += Repaint;
			ObjectListArea listArea2 = m_ListArea;
			listArea2.itemSelectedCallback += ListAreaItemSelectedCallback;
			m_ListArea.gridSize = m_StartGridSize.value;
		}
	}

	private void UpdateSearchResult(int currentSelection) {
		int[] instanceIDs = (from p in clips
							 where p.name.ToLower().Contains(m_SearchField.ToLower())
							 select p.GetInstanceID()).ToArray();
		m_ListArea.ShowObjectsInList(instanceIDs);
		m_ListArea.InitSelection(new int[1] { currentSelection });
	}

	private void ListAreaItemSelectedCallback(bool doubleClicked) {
		if (doubleClicked) {
			Close();
			GUIUtility.ExitGUI();
		}
	}

	private void OnGUI() {
		m_ListArea.HandleKeyboard(checkKeyboardControl: false);
		HandleKeyInput();
		EditorGUI.FocusTextInControl("ComponentSearch");
		DrawSearchField();
		Rect controlRect = EditorGUILayout.GetControlRect(true, GUILayout.ExpandHeight(expand: true));
		int controlID = GUIUtility.GetControlID(FocusType.Keyboard);
		m_ListArea.OnGUI(new Rect(0f, controlRect.y, base.position.width, controlRect.height), controlID);
		using (new EditorGUILayout.HorizontalScope(Style.bottomBarBg, GUILayout.MinHeight(24f))) {
			GUILayout.FlexibleSpace();
			if (m_ListArea.CanShowThumbnails()) {
				using (EditorGUI.ChangeCheckScope changeCheckScope = new EditorGUI.ChangeCheckScope()) {
					int gridSize = (int) GUILayout.HorizontalSlider(m_ListArea.gridSize, m_ListArea.minGridSize, m_ListArea.maxGridSize, GUILayout.Width(55f));
					if (changeCheckScope.changed) {
						m_ListArea.gridSize = gridSize;
					}
				}
			}
		}
	}

	private void DrawSearchField() {
		using (EditorGUI.ChangeCheckScope changeCheckScope = new EditorGUI.ChangeCheckScope()) {
			GUI.SetNextControlName("ComponentSearch");
			Rect controlRect = EditorGUILayout.GetControlRect(false, 24f, Style.toolbarBack);
			controlRect.height = 40f;
			GUI.Label(controlRect, GUIContent.none, Style.toolbarBack);
			m_SearchField = EditorGUI.SearchField(new Rect(5f, 5f, base.position.width - 10f, 15f), m_SearchField);
			if (changeCheckScope.changed) {
				UpdateSearchResult(0);
			}
		}
	}

	private void HandleKeyInput() {
		if (Event.current.type != EventType.KeyDown) {
			return;
		}
		switch (Event.current.keyCode) {
			case KeyCode.Escape:
				if (m_SearchField == string.Empty) {
					Cancel();
				}
				break;
			case KeyCode.Return:
			case KeyCode.KeypadEnter:
				Close();
				Event.current.Use();
				GUIUtility.ExitGUI();
				break;
		}
	}

	private void OnDisable() {
		if (m_ListArea != null) {
			m_StartGridSize.value = m_ListArea.gridSize;
		}
		onSelect?.Invoke(GetCurrentSelection());
		Undo.CollapseUndoOperations(m_ModalUndoGroup);
	}

	private void OnDestroy() {
		if (m_ListArea != null) {
			m_ListArea.OnDestroy();
		}
	}

	private AnimationClip GetCurrentSelection() {
		AnimationClip result = null;
		if (m_ListArea != null) {
			int[] selection = m_ListArea.GetSelection();
			if (selection != null && selection.Length != 0) {
				result = (EditorUtility.InstanceIDToObject(selection[0]) as AnimationClip);
			}
		}
		return result;
	}

	private void Cancel() {
		Undo.RevertAllDownToGroup(m_ModalUndoGroup);
		m_ListArea.InitSelection(new int[0]);
		Close();
		GUI.changed = true;
		GUIUtility.ExitGUI();
	}
}
