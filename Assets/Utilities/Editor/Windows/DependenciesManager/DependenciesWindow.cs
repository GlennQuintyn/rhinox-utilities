﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Rhinox.GUIUtils;
using Rhinox.GUIUtils.Attributes;
using Rhinox.GUIUtils.Editor;
using Rhinox.Lightspeed;
using Rhinox.Lightspeed.Reflection;
using Rhinox.Utilities.Editor;
using Sirenix.OdinInspector;
#if ODIN_INSPECTOR
using Sirenix.Utilities.Editor;
#endif
#if TEXT_MESH_PRO
using TMPro;
#endif
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Rhinox.Utilities.Editor
{
	public class DependenciesWindow : MenuListEditorWindow
	{
		// =================================================================================================================
		// PROPERTIES
		// =================================================================================================================

		public static GUIContent TitleContent => new GUIContent("Dependencies List",
#if ODIN_INSPECTOR
			EditorIcons.MagnifyingGlass.Raw
#else
			UnityIcon.AssetIcon("Fa_Search").Pad(5)
#endif
			);
		
		[HideInInspector] public DependencyHomePage HomePage;
		[HideInInspector] public AssetManager AssetManager;
		[HideInInspector] public DependenciesManager DependenciesManager = new DependenciesManager();
		[HideInInspector] public DependencySettings Settings;

		public bool ShowPath { get; private set; }

		private Object _currentSelection;

		private string _currentSelectedPath
		{
			get { return AssetDatabase.GetAssetPath(_currentSelection); }
		}

		private IReadOnlyList<DependencyAsset> _currentSelections;

		private string _selectionDescription;

		// =================================================================================================================
		// METHODS
		// =================================================================================================================
		// ASSET MANAGEMENT
		
		protected override void OnEnable()
		{
			base.OnEnable();
			Settings = new DependencySettings();
			AssetManager = new AssetManager();
			HomePage = new DependencyHomePage();
			HomePage.Initialize(this);
		}

		internal void ClearSelections()
		{
			_currentSelections = null;
		}


		// =================================================================================================================
		// Selection Management
		private Dependency[] GetSelection()
		{
			return MenuTree.Selection
				.Select(x => x.RawValue as Dependency)
				.Where(x => x != null)
				.OrderBy(x => x.Path)
				.ToArray();
		}

		private void OnSelectionChange()
		{
			if (Selection.instanceIDs.Length > 1)
				return;

			var selectedAssetPath = AssetDatabase.GetAssetPath(Selection.activeObject);

			if (_currentSelections != null && _currentSelections.Any(x => x.Path == selectedAssetPath))
				_currentSelection = Selection.activeObject;
		}

		private void SetSelection(IReadOnlyList<DependencyAsset> dependencies)
		{
			_currentSelections = AssetManager.GetIntersecting(dependencies, Settings.IgnoredFileRegexs,
				Settings.IgnoredDirectoryRegexs);
			SetSelectedObjects(_currentSelections);
		}

		private void SetSelectedObjects(IEnumerable<DependencyAsset> dependencies)
		{
			var guids = dependencies
				.Select(asset => AssetDatabase.AssetPathToGUID(asset.Path))
				.ToArray();

			Selection.instanceIDs = GetInstanceIDFromGUID(guids).ToArray();
		}

		static IEnumerable<int> GetInstanceIDFromGUID(params string[] guids)
		{
			// var method = typeof(AssetDatabase).GetMethod("GetInstanceIDFromGUID", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
			// 
			// foreach (var guid in guids)
			// 	yield return (int) method.Invoke(null, new object[] { guid });

			foreach (var guid in guids)
			{
				var asset = AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GUIDToAssetPath(guid));
				if (asset)
					yield return asset.GetInstanceID();
			}
		}

		private void SetInverseSelection(IReadOnlyList<string> paths)
		{
			SetSelection(AssetManager.InverseOf(paths, Settings.IgnoredFileRegexs, Settings.IgnoredDirectoryRegexs));
		}

		private void SetInverseSelection(IEnumerable<Dependency> dependencies)
		{
			DependencyAsset[] dependenciesToSelect = AssetManager.InverseOf(dependencies, Settings.IgnoredFileRegexs,
				Settings.IgnoredDirectoryRegexs);
			SetSelection(dependenciesToSelect);
		}

		private void SelectOther(int offset = 1)
		{
			if (_currentSelections == null) return;


			var i = _currentSelections.FindIndex(x => x.Path == _currentSelectedPath);

			if (i < 0) return;

			SetActiveSelection(_currentSelections.GetAtIndex(i + offset));
		}

		private void SetActiveSelection(DependencyAsset asset)
		{
			Selection.activeObject = asset != null ? asset.GetLoadedReference() : null;
		}

		private void SelectDirectory(int offset = 1)
		{
			if (_currentSelections == null) return;

			var i = _currentSelections.FindIndex(x => x.Path == _currentSelectedPath);

			string newDir = null;

			if (offset > 0)
			{
				var prevDirs = new HashSet<string>(_currentSelections.Take(i + offset).Select(x => x.Directory));
				newDir = _currentSelections.Skip(i).FirstOrDefault(x => !prevDirs.Contains(x.Directory))?.Directory;

				if (string.IsNullOrWhiteSpace(newDir))
					newDir = _currentSelections.FirstOrDefault()?.Directory;
			}
			else if (offset < 0)
			{
				var nextDirs = new HashSet<string>(_currentSelections.Skip(i).Select(x => x.Directory));
				for (i += offset; i >= 0; --i)
				{
					var item = _currentSelections[i];
					if (nextDirs.Contains(item.Directory)) continue;
					newDir = item.Directory;
					break;
				}

				if (string.IsNullOrWhiteSpace(newDir))
				{
					var selection = _currentSelections.LastOrDefault();
					newDir = selection?.Directory;
				}
			}
			else
				Debug.LogError("This offset is not yet defined!");

			if (!string.IsNullOrWhiteSpace(newDir))
				SetSelectedObjects(_currentSelections.Where(x => x.Directory == newDir));

			_currentSelection = Selection.activeObject;

		}

		private void RestoreSelection()
		{
			var currentFolder = eUtility.GetShownFolder();

			SetSelectedObjects(_currentSelections);

			if (!string.IsNullOrWhiteSpace(currentFolder) && _currentSelections.Any(x => x.Directory == currentFolder))
				EditorApplication.delayCall += () => eUtility.ShowFolderContents(currentFolder);
		}

		// overrides the way a selected item is shown when multiple are selected
		protected override object TransformTarget(object item)
		{
			if (MenuTree.Selection.Count > 1)
				return (item as Dependency)?.GetLoadedReference();

			return item;
		}

		// =================================================================================================================
		// GUI METHODS

		#region GUI Methods
		[MenuItem(WindowHelper.FindToolsPrefix + "Find Dependencies", false, -99)]
		public static void ShowWindow()
		{
			var w = GetWindow<DependenciesWindow>();
			w.titleContent = TitleContent;
			w.position = CustomEditorGUI.GetEditorWindowRect().AlignCenter(800, 600);

			w.Settings.Load();
		}

		protected override CustomMenuTree BuildMenuTree()
		{
			var tree = new CustomMenuTree();
#if ODIN_INSPECTOR
			// TODO: enable
			//tree.DefaultMenuStyle.IconSize = 16.00f;
			//tree.Config.DrawSearchToolbar = true;

			Texture homeIcon = EditorIcons.House.Raw;
			Texture listIcon = EditorIcons.List.Raw;
			Texture settingsIcon = EditorIcons.SettingsCog.Raw;
#else
			const int padding = 8;
			Texture homeIcon = UnityIcon.AssetIcon("Fa_Home").Pad(padding);
			Texture listIcon = UnityIcon.AssetIcon("Fa_ListUI").Pad(padding);
			Texture settingsIcon = UnityIcon.AssetIcon("Fa_Cog").Pad(padding);
#endif
			
			tree.Add("Home", HomePage, homeIcon);
			tree.Add("All Assets", AssetManager, listIcon);
			tree.Add("Settings", Settings, settingsIcon);

			foreach (var d in DependenciesManager.Dependencies)
			{
				if (!AssetManager.AllAssets.Contains(d.Path)) 
					continue;
				
				tree.Add(ShowPath ? d.PathNoAssets : d.Name, d, GetIconForType(d.Type));
			}

			return tree;
		}
		


		private Texture GetIconForType(Type t)
		{
			if (t == null) return null;
			
			var tex = Settings.IconMapper.ContainsKey(t) ? Settings.IconMapper[t] : null;
			if (tex) return tex;
			foreach (var type in Settings.IconMapper.Keys)
			{
				if (t.InheritsFrom(type))
					return Settings.IconMapper[type];
			}

			return null;
		}

		protected override void OnBeginDrawEditors()
		{
			var toolbarHeight = this.MenuTree.ToolbarHeight;

			CustomEditorGUI.BeginHorizontalToolbar(toolbarHeight);

			DrawToolbarBtns();

			GUILayout.FlexibleSpace();
			var newTerm = CustomEditorGUI.ToolbarSearchField(AssetManager.SearchText);
			if (AssetManager.CheckChange(newTerm))
				ForceMenuTreeRebuild();

			CustomEditorGUI.EndHorizontalToolbar();

			// Secondary toolbar
			var height = toolbarHeight / 1.5f;
			CustomEditorGUI.BeginHorizontalToolbar(height, paddingTop: 4 + toolbarHeight);
			DrawTypeActions();
			DrawTypeSearchButtons(height);
			CustomEditorGUI.EndHorizontalToolbar();
		}

		protected override void OnGUI()
		{
			GUILayout.BeginVertical();
			base.OnGUI();
			GUILayout.EndVertical();

			if (_currentSelections == null)
				return;

			const int spacer = 10;

			// Selection manager Toolbar
			CustomEditorGUI.BeginHorizontalToolbar(20);

			GUILayout.Space(spacer);

			if (_currentSelection != null)
			{
				if (CustomEditorGUI.ToolbarButton(new GUIContent("<<", tooltip: "Select all in previous folder")))
					SelectDirectory(-1);
				GUILayout.Space(spacer);
				if (CustomEditorGUI.ToolbarButton(new GUIContent("<", tooltip: "Select previous asset")))
					SelectOther(-1);
				GUILayout.Space(spacer);
			}

			if (CustomEditorGUI.ToolbarButton("Restore Selection")) RestoreSelection();

			if (_currentSelection != null)
			{
				GUILayout.Space(spacer);
				if (CustomEditorGUI.ToolbarButton(new GUIContent(">", tooltip: "Select next asset"))) SelectOther(1);
				GUILayout.Space(spacer);
				if (CustomEditorGUI.ToolbarButton(new GUIContent(">>", tooltip: "Select all in next folder")))
					SelectDirectory(1);
			}

			GUILayout.FlexibleSpace();

			EditorGUILayout.BeginVertical();
			GUILayout.FlexibleSpace();
			EditorGUILayout.LabelField(_selectionDescription);
			GUILayout.FlexibleSpace();
			EditorGUILayout.EndVertical();

			GUILayout.Space(spacer);
			CustomEditorGUI.EndHorizontalToolbar();
		}

		#endregion GUI Methods

		// =================================================================================================================
		// TOOLBAR METHODS

		#region TOOLBAR METHODS

		private void DrawTypeSearchButtons(float height)
		{
			GUILayout.FlexibleSpace();

			var changed = false;

			var searchPieces = AssetManager.SearchText.Split();
			var activeType = searchPieces.FirstOrDefault(x => x.StartsWith("t:"))?.Split(':').Last();

			var toggleOpts = new GUILayoutOption[] { GUILayout.Height(height), GUILayout.Width(height * 1.5f) };

			if (Settings != null && Settings.IconMapper != null)
			{
				foreach (var pair in Settings.IconMapper)
				{
					if (pair.Key == typeof(Texture2D)
#if TEXT_MESH_PRO
					    || pair.Key == typeof(TMP_FontAsset)
#endif
					   )
						continue;

					var typeName = pair.Key.Name;
					var content = new GUIContent(pair.Value, typeName);
					if (GUILayout.Toggle(activeType == typeName, content, CustomGUIStyles.ToolbarTab, toggleOpts))
					{
						activeType = typeName;
						searchPieces = searchPieces
							.Where(x => !x.StartsWith("t:"))
							.Append("t:" + activeType)
							.ToArray();

						var newSearch = string.Join(" ", searchPieces);

						if (AssetManager.CheckChange(newSearch))
							changed = true;
					}
				}
			}

			if (changed)
				ForceMenuTreeRebuild();
		}

		private void DrawToolbarBtns()
		{
			// TOGGLES
			var prev = ShowPath;
			
			ShowPath = GUILayout.Toggle(ShowPath, new GUIContent("Show path", UnityIcon.AssetIcon("Fa_Folder")), CustomGUIStyles.ToolbarButtonCentered, 
				GUILayout.MinWidth(22), GUILayout.Height(22));

			if (prev != ShowPath)
				ForceMenuTreeRebuild();

			if (!DependenciesManager.Dependencies.Any())
				return;

			// BUTTONS
			if (CustomEditorGUI.ToolbarButton("Select ALL"))
			{
				SetSelection(DependenciesManager.Dependencies);
				_selectionDescription = $"ALL ({Selection.instanceIDs.Length})";
			}

			if (CustomEditorGUI.ToolbarButton("Inverse ALL"))
			{
				SetInverseSelection(DependenciesManager.Dependencies);
				_selectionDescription = $"ALL INVERSE ({Selection.instanceIDs.Length})";
			}

			var selection = GetSelection();

			if (selection.Any() && CustomEditorGUI.ToolbarButton("Select"))
			{
				SetSelection(selection);
				_selectionDescription = $"selection ({Selection.instanceIDs.Length})";
			}

			if (selection.Any() && CustomEditorGUI.ToolbarButton("Inverse Select"))
			{
				SetInverseSelection(selection.Select(x => x.Path).ToArray());
				_selectionDescription = $"selection INVERSE ({Selection.instanceIDs.Length})";
			}
		}

		private Material _replacementMaterial;

		private void DrawTypeActions()
		{
			var selection = GetSelection();

			var type = selection.FirstOrDefault()?.Type;
			if (selection.Any(x => x.Type != type)) return;

			if (type == typeof(Material))
			{
				GUILayout.BeginHorizontal();
				if (GUILayout.Button("Replace Material with:"))
				{
					if (_replacementMaterial == null)
						Debug.LogError("You must fill in a Replacement Material first!");
					else
					{
						foreach (var dependency in selection)
						{
							var mat = dependency.GetLoadedReference() as Material;
							ReplaceMaterialInPrefabs(mat, _replacementMaterial,
								dependency.Users.OfType<GameObject>().ToArray());
						}
					}
				}

				_replacementMaterial = (Material) EditorGUILayout.ObjectField("", _replacementMaterial,
					typeof(Material), allowSceneObjects: false);
				GUILayout.EndHorizontal();
			}
		}

		private void ReplaceMaterialInPrefabs(Material old, Material newMaterial, params GameObject[] objects)
		{
			var prefabs = objects
				// .Select(go => PrefabUtility.GetCorrespondingObjectFromSource(go))
				.Where(x => x != null)
				.ToArray();

			ReplaceMaterial(old, newMaterial, prefabs);

			AssetDatabase.SaveAssets();
		}

		private void ReplaceMaterial(Material old, Material newMaterial, params GameObject[] objects)
		{
			foreach (var go in objects)
			{
				foreach (var r in go.GetComponentsInChildren<Renderer>())
				{
					var mats = r.sharedMaterials;
					if (!mats.Contains(old)) continue;

					for (int i = 0; i < mats.Length; ++i)
					{
						if (mats[i] == old)
							mats[i] = newMaterial;
					}

					r.materials = mats;
				}
			}
		}

		#endregion

		public void Clear()
		{
			
			DependenciesManager.Dependencies.Clear();
			ClearSelections();

			_selectionDescription = null;

			ForceMenuTreeRebuild();
		}
	}
}