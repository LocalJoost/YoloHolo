using System;
using UnityEditor;
using UnityEngine;

namespace MagicLeap.SetupTool.Editor.Utilities
{
    public static class DefineSymbolUtility
    {
        private static bool IsObsolete(BuildTargetGroup group)
		{
			var attrs = typeof(BuildTargetGroup).GetField(group.ToString()).GetCustomAttributes(typeof(ObsoleteAttribute), false);
			return attrs.Length > 0;
		}

		public static void RemoveDefineSymbol(string define)
		{
			foreach (BuildTargetGroup targetGroup in Enum.GetValues(typeof(BuildTargetGroup)))
			{
				if (targetGroup == BuildTargetGroup.Unknown || IsObsolete(targetGroup)) continue;

				var defineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);

				if (defineSymbols.Contains(define))
				{
					defineSymbols = defineSymbols.Replace($"{define};", "");
					defineSymbols = defineSymbols.Replace(define, "");

					PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, defineSymbols);
				}
			}
		}

		public static void AddDefineSymbol(string define)
		{
	
			foreach (BuildTargetGroup targetGroup in Enum.GetValues(typeof(BuildTargetGroup)))
			{
				if (targetGroup == BuildTargetGroup.Unknown || IsObsolete(targetGroup)) continue;

				var defineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);

				if (!defineSymbols.Contains(define))
				{
					if (defineSymbols.Length < 1)
						defineSymbols = define;
					else if (defineSymbols.EndsWith(";"))
						defineSymbols = $"{defineSymbols}{define}";
					else
						defineSymbols = $"{defineSymbols};{define}";

					PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, defineSymbols);
				}
			}
		}

		public static bool ContainsDefineSymbolInAllBuildTargets(string symbol)
		{
			
			bool contains = false;
			foreach (BuildTargetGroup targetGroup in Enum.GetValues(typeof(BuildTargetGroup)))
			{
				if (targetGroup== BuildTargetGroup.EmbeddedLinux || targetGroup== BuildTargetGroup.LinuxHeadlessSimulation || targetGroup == BuildTargetGroup.Unknown || IsObsolete(targetGroup)) continue;

				var defineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);
				contains = defineSymbols.Contains(symbol);
				if (!contains)
				{
			
					break;
				}
			}

			return contains;
		}
		public static bool ContainsDefineSymbolInAnyBuildTarget(string symbol)
		{
			bool contains = false;
			foreach (BuildTargetGroup targetGroup in Enum.GetValues(typeof(BuildTargetGroup)))
			{
				if (targetGroup == BuildTargetGroup.Unknown || IsObsolete(targetGroup)) continue;

				var defineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);
				contains = defineSymbols.Contains(symbol);
				if (contains)
				{
					break;
				}
			}

			return contains;
		}
    }
}