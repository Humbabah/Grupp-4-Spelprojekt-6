﻿using System;
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Text;

public static class Writer
{
    public static void WriteTo(this BinaryWriter bin, PrefabInstanceData data)
    {
        bin.Write(data.myInstanceID);
        bin.Write(data.myPosition.x);
        bin.Write(data.myPosition.y);
        bin.Write(data.myPosition.z);
        bin.Write(data.myRotation.x);
        bin.Write(data.myRotation.y);
        bin.Write(data.myRotation.z);
        bin.Write(data.myScale.x);
        bin.Write(data.myScale.y);
        bin.Write(data.myScale.z);
        bin.Write(data.myModelIndex);
    }
    public static void WriteTo(this BinaryWriter bin, CameraData data)
    {
        bin.Write(data.myPosX);
        bin.Write(data.myPosY);
        bin.Write(data.myPosZ);
        bin.Write(data.myRotX);
        bin.Write(data.myRotY);
        bin.Write(data.myRotZ);
        bin.Write(data.myFieldOfView);
        bin.Write(data.myStartInCameraMode);
        bin.Write(data.myToggleFreeCamKey);
        bin.Write(data.myFreeCamMoveSpeed);
        bin.Write(data.offset.x);
        bin.Write(data.offset.y);
        bin.Write(data.offset.z);
    }
    public static void WriteTo(this BinaryWriter bin, DirectionalLightData data)
    {
        bin.Write(data.myDirectionX);
        bin.Write(data.myDirectionY);
        bin.Write(data.myDirectionZ);
        bin.Write(data.myColorR);
        bin.Write(data.myColorG);
        bin.Write(data.myColorB);
        bin.Write(data.myIntensity);
    }
    public static void WriteTo(this BinaryWriter bin, PointLightData data)
    {
        bin.Write(data.myInstanceID);
        bin.Write(data.myPosition.x);
        bin.Write(data.myPosition.y);
        bin.Write(data.myPosition.z);
        bin.Write(data.myRange);
        bin.Write(data.myColor.x);
        bin.Write(data.myColor.y);
        bin.Write(data.myColor.z);
        bin.Write(data.myIntensity);
    }
    public static void WriteTo(this BinaryWriter bin, PlayerData data)
    {
        bin.Write(data.myInstanceID);
        bin.Write(data.myPosition.x);
        bin.Write(data.myPosition.y);
        bin.Write(data.myPosition.z);
        bin.Write(data.myRotation.x);
        bin.Write(data.myRotation.y);
        bin.Write(data.myRotation.z);
        bin.Write(data.myScale.z);
        bin.Write(data.myScale.z);
        bin.Write(data.myScale.z);
        bin.Write(data.myModelIndex);
        //Player health osv
    }
}

public class BinaryExporter
{
    [MenuItem("Tools/Export all BIN #_y")]
    private static void DoExportBinary()
    {

        MeshFilter[] allMeshes = GameObject.FindObjectsOfType<MeshFilter>();
        Debug.Log(allMeshes.Length);


        Debug.Log("Exporting Binary");

        string target_path = "..\\IronWrought\\Bin\\Levels\\";
        if (!System.IO.Directory.Exists(target_path))
        {
            System.IO.Directory.CreateDirectory(target_path);
        }

        string scene_name = SceneManager.GetActiveScene().name;

        // Save paths
        List<string> modelPaths = new List<string>();
        GameObject[] allModels = UnityEngine.Object.FindObjectsOfType<GameObject>();
        foreach (GameObject go in allModels)
        {
            MeshFilter mesh = go.GetComponentInChildren<MeshFilter>();
            if (mesh != null)
            {
                MeshFilter originalSource = PrefabUtility.GetCorrespondingObjectFromOriginalSource(mesh);
                string path = AssetDatabase.GetAssetPath(originalSource);
                if (modelPaths.Contains(path))
                    continue;

                modelPaths.Add(path);
            }
        }

        using (System.IO.StreamWriter file = new System.IO.StreamWriter(target_path + scene_name + "_bin_modelPaths.json"))
        {
            int count = 0;
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("{");
            stringBuilder.AppendLine("\"ModelPaths\" : [");
            foreach (string entry in modelPaths)
            {
                stringBuilder.AppendLine("{");

                stringBuilder.Append("\"Path\" : ");
                stringBuilder.Append("\"");
                stringBuilder.Append(entry);
                stringBuilder.AppendLine("\"");

                stringBuilder.AppendLine("}");
                count++;
                if (count != modelPaths.Count)
                {
                    stringBuilder.Append(",");
                }
            }
            stringBuilder.AppendLine("]");
            stringBuilder.AppendLine("}");
            file.Write(stringBuilder.ToString());
        }


        BinaryWriter bw;
        bw = new BinaryWriter(new FileStream(target_path + scene_name + "_bin.bin", FileMode.Create));

        GameObject[] allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();

        Camera camera = UnityEngine.Object.FindObjectOfType<Camera>();
        if (camera == null)
        {
            Debug.LogWarning("No Camera found! Please add one before you export!");
        }
        CameraData cameraData = new CameraData(camera);
        bw.WriteTo(cameraData);

        Light[] lights = UnityEngine.Object.FindObjectsOfType<Light>();
        List<Light> pointLights = new List<Light>();
        Light directionalLight = null;
        foreach (Light light in lights)
        {
            if (light.type == LightType.Directional)
            {
                directionalLight = light;
            }
            else if (light.type == LightType.Point)
            {
                pointLights.Add(light);
            }
        }

        if (directionalLight == null)
        {
            Debug.LogWarning("No directionlight found! Please add one before you export!");
        }

        DirectionalLightData directionalLightData = new DirectionalLightData(directionalLight);
        bw.WriteTo(directionalLightData);


        bw.Write(pointLights.Count);
        if (pointLights.Count > 0)
        {
            foreach (Light pointLight in pointLights)
            {
                bw.WriteTo(new PointLightData(pointLight));
            }
        }

        Player player = UnityEngine.Object.FindObjectOfType<Player>();
        if (player != null)
        {
            bw.Write(1);
            PlayerData playerData = new PlayerData(player, GetModelIndexFromPrefab(player.gameObject, modelPaths));
            bw.WriteTo(playerData);

        }
        else
        {
            bw.Write(0);
            Debug.LogWarning("No Player found! Please add one before you export! If this the loadscene you can ignore this warning.");
        }


        MeshFilter[] allPrefabMeshes = GameObject.FindObjectsOfType<MeshFilter>();

        List<PrefabInstanceData> prefabInstanceDataList = new List<PrefabInstanceData>();
        foreach (MeshFilter mesh in allPrefabMeshes)
        {
            if (mesh.GetComponentInParent<Player>() != null)
                continue;

            GameObject prefabParent = PrefabUtility.GetNearestPrefabInstanceRoot(mesh);
            int index = Getindex(mesh.gameObject, modelPaths);
            prefabInstanceDataList.Add(new PrefabInstanceData(prefabParent, index));
        }

        int prefabInstanceCount = prefabInstanceDataList.Count;
        bw.Write(prefabInstanceCount);
        Debug.Log("Gameobjects with Models Exported: " + prefabInstanceCount);
        foreach (PrefabInstanceData data in prefabInstanceDataList)
        {
            bw.WriteTo(data);
        }

        bw.Close();
    }

    private static int GetModelIndexFromPrefab(GameObject gameObject, List<string> filter)
    {
        return Getindex(PrefabUtility.GetCorrespondingObjectFromOriginalSource<GameObject>(gameObject.transform.GetChild(0).gameObject), filter);
    }

    private static int Getindex(GameObject go, List<string> modelPaths)
    {
        int index = -1;
        foreach (string entry in modelPaths)
        {
            index++;

            string path = AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromOriginalSource(go));

            if (entry == path)
            {
                break;
            }

            // do something with entry.Value or entry.Key
        }
        return index;
    }

}