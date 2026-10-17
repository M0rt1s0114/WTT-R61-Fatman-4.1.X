using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using Diz.Resources;
using EFT.AssetsManager;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FatmanShaderFix;

[BepInPlugin("com.codex.fatmanshaderfix", "Fatman material compatibility fix", "0.9.2")]
[BepInDependency("com.codex.mfacshaderfixtest", BepInDependency.DependencyFlags.SoftDependency)]
public sealed class Plugin : BaseUnityPlugin
{
	private sealed class MaterialSnapshot
	{
		internal readonly Dictionary<string, Texture?> Textures = new Dictionary<string, Texture>();

		internal readonly Dictionary<string, Vector2> TextureScales = new Dictionary<string, Vector2>();

		internal readonly Dictionary<string, Vector2> TextureOffsets = new Dictionary<string, Vector2>();

		internal readonly Dictionary<string, Color> Colors = new Dictionary<string, Color>();

		internal readonly Dictionary<string, float> Floats = new Dictionary<string, float>();
	}

	internal static Plugin Instance = null!;

	internal static ManualLogSource Log = null!;

	private const string ReplacementShaderName = "Tarkov Replacements/Standard SMAP Metallic Replacement";

	private const string FallbackShaderName = "Rendering/Bump Spec SMAP Equivalent";

	private static readonly List<Shader> LoadedCompatShaders = new List<Shader>();

	private static readonly HashSet<int> FixedMaterials = new HashSet<int>();

	private static readonly Dictionary<string, Object[]> PendingFatmanBundles = new Dictionary<string, Object[]>();

	private static AssetBundle _bundledShaderBundle;

	private static bool _describedReplacement;

	private void Awake()
	{
		Instance = this;
		Log = Logger;
		RemoveKnownConflictingPatch();
		LoadBundledShaders();
		HookBundleLoad();
		Log.LogInfo((object)"Fatman material fix 0.9.2: bundled SMAP shaders; one-shot rebinding; no polling.");
	}

	private static void RemoveKnownConflictingPatch()
	{
		try
		{
			Harmony.UnpatchID("com.codex.mfacshaderfixtest");
			Log.LogInfo((object)"Removed legacy MFAC material patch if present; Fatman uses one material path only.");
		}
		catch (Exception ex)
		{
			Log.LogWarning((object)("Could not remove legacy MFAC patch: " + ex.Message));
		}
	}

	private static void LoadBundledShaders()
	{
		try
		{
			var pluginDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			var shaderBundlePath = Path.Combine(pluginDirectory ?? string.Empty, "c11_shaders.bundle");
			if (!File.Exists(shaderBundlePath))
			{
				Log.LogWarning((object)$"Bundled shader asset not found: {shaderBundlePath}");
				return;
			}

			_bundledShaderBundle = AssetBundle.LoadFromFile(shaderBundlePath);
			if (_bundledShaderBundle == null)
			{
				Log.LogWarning((object)$"Failed to load bundled shader asset: {shaderBundlePath}");
				return;
			}

			CacheShaders(_bundledShaderBundle.LoadAllAssets(), "FatmanShaderFix/c11_shaders.bundle");
		}
		catch (Exception ex)
		{
			Log.LogWarning((object)("Loading bundled shaders failed: " + ex));
		}
	}

	private void HookBundleLoad()
	{
		try
		{
			var harmony = new Harmony("com.codex.fatmanshaderfix");

			TryPatch(harmony,
				typeof(EasyBundle),
				"set_Assets",
				[typeof(Object[])],
				postfix: new HarmonyMethod(typeof(Plugin), nameof(OnEasyBundleAssetsLoaded)));

			TryPatch(harmony,
				typeof(AssetsManager.LoadAssetOperation),
				"LoadContent",
				[typeof(AssetsManager), typeof(string), typeof(Object)],
				postfix: new HarmonyMethod(typeof(Plugin), nameof(OnSingleAssetLoaded)));

			TryPatch(harmony,
				typeof(AssetsManager.LoadAllAssetOperation),
				"LoadContent",
				[typeof(AssetsManager), typeof(AssetBundle), typeof(Object[])],
				postfix: new HarmonyMethod(typeof(Plugin), nameof(OnAllAssetsLoaded)));

			TryPatch(harmony,
				typeof(AssetsManager),
				nameof(AssetsManager.LoadAssetAsync),
				[typeof(string), typeof(string)],
				prefix: new HarmonyMethod(typeof(Plugin), nameof(OnLoadAssetRequested)));

			TryPatch(harmony,
				typeof(AssetsManager),
				nameof(AssetsManager.LoadMainAssetAsync),
				[typeof(string)],
				prefix: new HarmonyMethod(typeof(Plugin), nameof(OnLoadMainAssetRequested)));

			TryPatch(harmony,
				typeof(AssetsManager),
				nameof(AssetsManager.LoadAssetsAsync),
				[typeof(string)],
				prefix: new HarmonyMethod(typeof(Plugin), nameof(OnLoadAssetsRequested)));

			TryPatch(harmony,
				typeof(AssetsManager),
				nameof(AssetsManager.LoadBundlesAsync),
				[typeof(string[])],
				prefix: new HarmonyMethod(typeof(Plugin), nameof(OnLoadBundlesRequested)));
		}
		catch (Exception ex)
		{
			Log.LogWarning((object)("Hook failed: " + ex.Message));
		}
	}

	private static void OnEasyBundleAssetsLoaded(EasyBundle __instance, Object[] value)
	{
		if (__instance == null || value == null || value.Length == 0)
		{
			return;
		}

		ProcessBundleAssets(__instance.Key, value);
	}

	private static void TryPatch(
		Harmony harmony,
		Type type,
		string methodName,
		Type[] parameters,
		HarmonyMethod prefix = null,
		HarmonyMethod postfix = null)
	{
		var method = AccessTools.Method(type, methodName, parameters);
		if (method == null)
		{
			Log.LogWarning((object)$"Hook target not found: {type.FullName}.{methodName}");
			return;
		}

		harmony.Patch(method, prefix, postfix);
		Log.LogInfo((object)$"Hooked {type.FullName}.{methodName}");
	}

	private static bool IsRelevantBundle(string value, out string lower)
	{
		lower = string.IsNullOrEmpty(value) ? string.Empty : value.ToLowerInvariant();
		return lower.Contains("wtt_shaders")
			|| lower.Contains("fatman")
			|| lower.Contains("heavy_assult");
	}

	private static void OnLoadAssetRequested(string bundleName, string assetName)
	{
		if (IsRelevantBundle(bundleName, out _))
		{
			Log.LogInfo((object)$"[Fatman] asset request: bundle={bundleName} asset={assetName}");
		}
	}

	private static void OnLoadMainAssetRequested(string bundleName)
	{
		if (IsRelevantBundle(bundleName, out _))
		{
			Log.LogInfo((object)$"[Fatman] main-asset request: bundle={bundleName}");
		}
	}

	private static void OnLoadAssetsRequested(string bundleName)
	{
		if (IsRelevantBundle(bundleName, out _))
		{
			Log.LogInfo((object)$"[Fatman] all-assets request: bundle={bundleName}");
		}
	}

	private static void OnLoadBundlesRequested(string[] bundleNames)
	{
		if (bundleNames == null)
		{
			return;
		}

		foreach (var bundleName in bundleNames)
		{
			if (IsRelevantBundle(bundleName, out _))
			{
				Log.LogInfo((object)$"[Fatman] bundle-list request includes: {bundleName}");
			}
		}
	}

	private static void OnSingleAssetLoaded(AssetsManager.LoadAssetOperation __instance, Object asset)
	{
		if (__instance == null || asset == null)
		{
			return;
		}

		ProcessBundleAssets(__instance.BundleName, [asset]);
	}

	private static void OnAllAssetsLoaded(AssetBundle bundle, Object[] assets)
	{
		if (bundle == null || assets == null || assets.Length == 0)
		{
			return;
		}

		ProcessBundleAssets(bundle.name, assets);
	}

	private static void ProcessBundleAssets(string key, Object[] assets)
	{
		try
		{
			if (!IsRelevantBundle(key, out var lower) || assets == null || assets.Length == 0)
			{
				return;
			}

			Log.LogInfo((object)$"[Fatman] callback: bundle={key} assets={assets.Length}");

			if (lower.Contains("wtt_shaders"))
			{
				CacheShaders(assets, key);
				ProcessPendingFatmanBundles();
			}
			else
			{
				if ((Object)(object)FindReplacementShader() == (Object)null)
				{
					PendingFatmanBundles[key] = assets;
					Log.LogWarning((object)("[Fatman] WTT replacement shader is not ready for " + key + "; queued once for dependency completion."));
				}
				else
				{
					FixFatmanAssets(assets, key);
				}
			}
		}
		catch (Exception ex)
		{
			ManualLogSource log = Log;
			if (log != null)
			{
				log.LogWarning((object)("Bundle callback failed: " + ex.Message));
			}
		}
	}

	private static void CacheShaders(Object[] assets, string key)
	{
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		int num = 0;
		foreach (Object obj in assets)
		{
			Shader val = (Shader)(object)((obj is Shader) ? obj : null);
			if (val != null && !((Object)(object)val == (Object)null))
			{
				if (!LoadedCompatShaders.Contains(val))
				{
					LoadedCompatShaders.Add(val);
				}
				num++;
			}
		}
		Log.LogInfo((object)$"[WTT shader] cached {num} shaders from {key}; total={LoadedCompatShaders.Count}");
		Shader val2 = FindReplacementShader();
		if ((Object)(object)val2 != (Object)null && !_describedReplacement)
		{
			_describedReplacement = true;
			Log.LogInfo((object)$"[WTT shader] selected '{((Object)val2).name}' supported={val2.isSupported} properties={val2.GetPropertyCount()}");
			for (int j = 0; j < val2.GetPropertyCount(); j++)
			{
				Log.LogInfo((object)$"[WTT shader] property {j}: {val2.GetPropertyName(j)} ({val2.GetPropertyType(j)})");
			}
		}
	}

	private static void ProcessPendingFatmanBundles()
	{
		if ((Object)(object)FindReplacementShader() == (Object)null || PendingFatmanBundles.Count == 0)
		{
			return;
		}
		List<KeyValuePair<string, Object[]>> list = new List<KeyValuePair<string, Object[]>>(PendingFatmanBundles);
		PendingFatmanBundles.Clear();
		foreach (KeyValuePair<string, Object[]> item in list)
		{
			FixFatmanAssets(item.Value, item.Key);
		}
	}

	private static Shader? FindReplacementShader()
	{
		foreach (Shader loadedCompatShader in LoadedCompatShaders)
		{
			if ((Object)(object)loadedCompatShader != (Object)null && loadedCompatShader.isSupported && string.Equals(((Object)loadedCompatShader).name, "Tarkov Replacements/Standard SMAP Metallic Replacement", StringComparison.Ordinal))
			{
				return loadedCompatShader;
			}
		}
		foreach (Shader loadedCompatShader2 in LoadedCompatShaders)
		{
			if ((Object)(object)loadedCompatShader2 != (Object)null && loadedCompatShader2.isSupported && string.Equals(((Object)loadedCompatShader2).name, "Rendering/Bump Spec SMAP Equivalent", StringComparison.Ordinal))
			{
				return loadedCompatShader2;
			}
		}
		return null;
	}

	private static void FixFatmanAssets(Object[] assets, string key)
	{
		Shader val = FindReplacementShader();
		if ((Object)(object)val == (Object)null)
		{
			return;
		}
		int num = 0;
		foreach (Object val2 in assets)
		{
			Material val3 = (Material)(object)((val2 is Material) ? val2 : null);
			if (val3 != null)
			{
				if (Rebind(val3, val))
				{
					num++;
				}
				continue;
			}
			GameObject val4 = (GameObject)(object)((val2 is GameObject) ? val2 : null);
			if (val4 == null)
			{
				continue;
			}
			FixBones(val4);
			Renderer[] componentsInChildren = val4.GetComponentsInChildren<Renderer>(true);
			for (int j = 0; j < componentsInChildren.Length; j++)
			{
				Material[] sharedMaterials = componentsInChildren[j].sharedMaterials;
				for (int k = 0; k < sharedMaterials.Length; k++)
				{
					if (Rebind(sharedMaterials[k], val))
					{
						num++;
					}
				}
			}
		}
		Log.LogInfo((object)$"[Fatman] {key}: rebound {num} invalid material(s) to {((Object)val).name}; no periodic work scheduled.");
	}

	private static bool Rebind(Material? material, Shader replacement)
	{
		if ((Object)(object)material == (Object)null || (Object)(object)replacement == (Object)null)
		{
			return false;
		}
		int instanceID = ((Object)material).GetInstanceID();
		if (FixedMaterials.Contains(instanceID))
		{
			return false;
		}
		if (!IsInvalid(material.shader))
		{
			return false;
		}
		MaterialSnapshot snapshot = CaptureProperties(material);
		material.shader = replacement;
		RestoreProperties(material, snapshot);
		FixedMaterials.Add(instanceID);
		return true;
	}

	private static bool IsInvalid(Shader? shader)
	{
		if ((Object)(object)shader == (Object)null)
		{
			return true;
		}
		if (string.Equals(((Object)shader).name, "Hidden/InternalErrorShader", StringComparison.Ordinal) || string.Equals(((Object)shader).name, "EFT.Visual", StringComparison.Ordinal))
		{
			return true;
		}
		return !shader.isSupported;
	}

	private static MaterialSnapshot CaptureProperties(Material material)
	{
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		MaterialSnapshot materialSnapshot = new MaterialSnapshot();
		string[] array = new string[8] { "_MainTex", "_BumpMap", "_SpecMap", "_GlossMap", "_MetallicGlossMap", "_OcclusionMap", "_DetailNormalMap", "_EmissionMap" };
		foreach (string text in array)
		{
			if (material.HasProperty(text))
			{
				materialSnapshot.Textures[text] = material.GetTexture(text);
				materialSnapshot.TextureScales[text] = material.GetTextureScale(text);
				materialSnapshot.TextureOffsets[text] = material.GetTextureOffset(text);
			}
		}
		array = new string[7] { "_Color", "_BaseTintColor", "_SpecColor", "_ReflectColor", "_EmissionColor", "_HeatColor1", "_HeatColor2" };
		foreach (string text2 in array)
		{
			if (material.HasProperty(text2))
			{
				materialSnapshot.Colors[text2] = material.GetColor(text2);
			}
		}
		array = new string[12]
		{
			"_BumpScale", "_Glossiness", "_Glossness", "_GlossMapScale", "_Metallic", "_NormalIntensity", "_NormalUVMultiplier", "_Specularness", "_SpecularHighlights", "_DropsSpec",
			"_GlossyReflections", "_SpecVals"
		};
		foreach (string text3 in array)
		{
			if (material.HasProperty(text3))
			{
				materialSnapshot.Floats[text3] = material.GetFloat(text3);
			}
		}
		return materialSnapshot;
	}

	private static void RestoreProperties(Material material, MaterialSnapshot snapshot)
	{
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		foreach (KeyValuePair<string, Texture> texture in snapshot.Textures)
		{
			if (material.HasProperty(texture.Key))
			{
				material.SetTexture(texture.Key, texture.Value);
				material.SetTextureScale(texture.Key, snapshot.TextureScales[texture.Key]);
				material.SetTextureOffset(texture.Key, snapshot.TextureOffsets[texture.Key]);
			}
		}
		foreach (KeyValuePair<string, Color> color in snapshot.Colors)
		{
			if (material.HasProperty(color.Key))
			{
				material.SetColor(color.Key, color.Value);
			}
		}
		foreach (KeyValuePair<string, float> @float in snapshot.Floats)
		{
			if (material.HasProperty(@float.Key))
			{
				material.SetFloat(@float.Key, @float.Value);
			}
		}
	}

	private static void FixBones(GameObject go)
	{
		Transform[] componentsInChildren = go.GetComponentsInChildren<Transform>(true);
		foreach (Transform val in componentsInChildren)
		{
			if (((Object)val).name == "mod_equipment_000.001")
			{
				((Object)val).name = "mod_equipment";
			}
		}
	}
}
