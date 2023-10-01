using System.Collections.Generic;
using System.Xml;
using UnityEngine;

namespace MagicLeap.SetupTool.Editor.Utilities
{
internal class AndroidManifest : AndroidXmlDocument
{
    private readonly XmlElement activityManifestElement;
    private readonly XmlElement ApplicationElement;

    private XmlNodeList permissionNodes;
    private XmlNodeList featureNodes;
    public AndroidManifest(string path) : base(path)
    {
  
        ApplicationElement = SelectSingleNode("/manifest/application") as XmlElement;
        permissionNodes = SelectNodes("manifest/uses-permission", _xmlNamespaceManager);
        featureNodes = SelectNodes("manifest/uses-feature", _xmlNamespaceManager);
        activityManifestElement = SelectSingleNode("/manifest") as XmlElement;
    }

    private XmlAttribute CreateAndroidAttribute(string key, string value)
    {
        XmlAttribute attr = CreateAttribute("android", key, AndroidXmlNamespace);
        attr.Value = value;
        return attr;
    }

    public List<string> GetIncludedPermissions()
    {

        if (permissionNodes != null)
        {
            List<string> permissions = new List<string>();
            for (int i = 0; i < permissionNodes.Count; i++)
            {
                var node = permissionNodes[i];
                string name = node.Attributes["android:name"].Value;
                permissions.Add(name);
            }

            return permissions;
        }

        return new List<string>();
    }

    public List<string> GetIncludedFeatures()
    {
        if (featureNodes != null)
        {
            List<string> features = new List<string>();
            for (var i = 0; i < featureNodes.Count; i++)
            {
                var node = featureNodes[i];
                string name = node.Attributes["android:name"].Value;
                features.Add(name);
            }

            return features;
        }

        return new List<string>();
    }
    public XmlNode GetActivityWithLaunchIntent()
    {
        return SelectSingleNode("/manifest/application/activity[intent-filter/action/@android:name='android.intent.action.MAIN' and " +
                "intent-filter/category/@android:name='android.intent.category.LAUNCHER']", _xmlNamespaceManager);
    }

    public void SetStartingActivityName(string activityName)
    {
        GetActivityWithLaunchIntent().Attributes.Append(CreateAndroidAttribute("name", activityName));
    }

    public void AddFeature(string featureName)
    {
        if(GetIncludedFeatures().IndexOf(featureName) >= 0)
        {
            // feature already exists
            return;
        }
        XmlElement child = CreateElement("uses-feature");
        activityManifestElement.AppendChild(child);
        
        XmlAttribute newAttribute = CreateAndroidAttribute("name", featureName);
        child.Attributes.Append(newAttribute);

    }

    public void RemoveFeature(string featureName)
    {
        if (GetIncludedFeatures().IndexOf(featureName) == -1)
        {
            // feature doesn't exists
            return;
        }

        var matchingNodes = new List<XmlNode>();
        for (var i = 0; i < featureNodes.Count; i++)
        {
            var node = featureNodes[i];
            if (node.Attributes != null && node.Attributes["android:name"].Value == featureName)
            {
                matchingNodes.Add(node);
            }
        }

        // remove all matching in case of duplicates
        foreach (var node in matchingNodes)
        {
            activityManifestElement.RemoveChild(node);
        }
    }
    public void AddPermission(string permissionName)
    {
        if (GetIncludedPermissions().IndexOf(permissionName) >= 0)
        {
            // permission already exists
            return;
        }

        XmlElement child = CreateElement("uses-permission");
        activityManifestElement.AppendChild(child);

        XmlAttribute newAttribute = CreateAndroidAttribute("name", permissionName);
        child.Attributes.Append(newAttribute);

    }

    public void RemovePermission(string permissionName)
    {
        if (GetIncludedPermissions().IndexOf(permissionName) == -1)
        {
            // permission doesn't exists
            return;
        }

        var matchingNodes = new List<XmlNode>();
        for (int i = 0; i < permissionNodes.Count; i++)
        {
            var node = permissionNodes[i];
            if (node.Attributes != null && node.Attributes["android:name"].Value == permissionName)
            {
                matchingNodes.Add(node);
            }
        }

        // remove all matching in case of duplicates
        foreach (var node in matchingNodes)
        {
            activityManifestElement.RemoveChild(node);
        }
    }

    public void SetHardwareAcceleration()
    {
        var xmlAttributeCollection = GetActivityWithLaunchIntent().Attributes;
        xmlAttributeCollection?.Append(CreateAndroidAttribute("hardwareAccelerated", "true"));
    }

    public void SetPermission(string permissionName, bool use)
    {
        if (use)
        {
            AddPermission(permissionName);
        }
        else
        {
            RemovePermission(permissionName);
        }
    }

    public void SetFeature(string featureName, bool use)
    {
        if (use)
        {
            AddFeature(featureName);
        }
        else
        {
            RemoveFeature(featureName);
        }
    }
    
    public void SetMicrophonePermission(bool use)
    {
        SetPermission("android.permission.RECORD_AUDIO", use);
    }

    public bool HasFeature(string featureName)
    {
        return (GetIncludedPermissions().IndexOf(featureName) >= 0);
    }
    
    public bool HasPermission(string permissionName)
    {
        return (GetIncludedPermissions().IndexOf(permissionName) >= 0);
    }

    public void SetBluetoothPermission(bool use)
    {
        SetPermission("android.permission.BLUETOOTH", use);
        SetPermission("android.permission.BLUETOOTH_ADMIN", use);
    }
    public void SetExtStoragePermission(bool use)
    {
        SetPermission("android.permission.READ_EXTERNAL_STORAGE", use);
    }

    public void SetAccessNetworkPermission(bool use)
    {
        SetPermission("android.permission.ACCESS_NETWORK_STATE", use);
    }

    public void SetWIFIPermission(bool use)
    {
        SetPermission("android.permission.ACCESS_WIFI_STATE", use);
    }

    public void SetManageDocumentsPermission(bool use)
    {
        SetPermission("android.permission.MANAGE_DOCUMENTS", use);
       
    }

    public void SetUSBHostFeature(bool use)
    {
        SetFeature("android.hardware.usb.host", use);
       
    }

    public void SetUSBIntents()
    {
        var activityIntentFilter = SelectSingleNode("/manifest/application/activity/intent-filter");
        XmlElement child = CreateElement("action");
        activityIntentFilter.AppendChild(child);
        XmlAttribute newAttribute = CreateAndroidAttribute("name", "android.hardware.usb.action.USB_DEVICE_ATTACHED");
        child.Attributes.Append(newAttribute);
        activityIntentFilter = SelectSingleNode("/manifest/application/activity/intent-filter");
        child = CreateElement("action");
        activityIntentFilter.AppendChild(child);
        newAttribute = CreateAndroidAttribute("name", "android.hardware.usb.action.USB_DEVICE_DETACHED");
        child.Attributes.Append(newAttribute);
    }

    public void SetUSBMetadata()
    {
        var applicationActivity = SelectSingleNode("/manifest/application/activity");
        XmlElement child = CreateElement("meta-data");
        applicationActivity.AppendChild(child);
        XmlAttribute newAttribute = CreateAndroidAttribute("name", "android.hardware.usb.action.USB_DEVICE_ATTACHED");
        child.Attributes.Append(newAttribute);
        newAttribute = CreateAndroidAttribute("resource", "@xml/device_filter");
        child.Attributes.Append(newAttribute);
        applicationActivity = SelectSingleNode("/manifest/application/activity");
        child = CreateElement("meta-data");
        applicationActivity.AppendChild(child);
        newAttribute = CreateAndroidAttribute("name", "android.hardware.usb.action.USB_DEVICE_DETACHED");
        child.Attributes.Append(newAttribute);
        newAttribute = CreateAndroidAttribute("resource", "@xml/device_filter");
        child.Attributes.Append(newAttribute);
    }
}
}
