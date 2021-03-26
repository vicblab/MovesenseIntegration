#if UNITY_IOS
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

public static class PostBuildTrigger {
    [PostProcessBuild]
    public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject) { 
        string projPath = pathToBuiltProject + "/Unity-iPhone.xcodeproj/project.pbxproj";
        PBXProject proj = new PBXProject();
        proj.ReadFromFile(projPath);
        string targetGUID = proj.TargetGuidByName("Unity-iPhone");
        proj.AddBuildProperty(targetGUID, "OTHER_LDFLAGS", "-ObjC"); 
        proj.AddFileToBuild(targetGUID, proj.AddFile("usr/lib/libz.tbd", "Frameworks/libz.tbd", PBXSourceTree.Sdk));
        proj.AddFileToBuild(targetGUID, proj.AddFile("usr/lib/libz.tbd", "Frameworks/libz.tbd", PBXSourceTree.Build));
        proj.WriteToFile(projPath);

        string plistPath = pathToBuiltProject + "/Info.plist";
        var plist = new PlistDocument();

        plist.ReadFromFile(plistPath);
        var rootDict = plist.root;

        rootDict.SetString("NSBluetoothPeripheralUsageDescription", "App needs Bluetooth to connect to Movesense devices");

        plist.WriteToFile(plistPath);
    }
}
#endif
