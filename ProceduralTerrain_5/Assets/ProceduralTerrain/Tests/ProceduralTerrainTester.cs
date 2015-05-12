using UnityEngine;
using System.Collections;

using VTL.ProceduralTerrain;

public class ProceduralTerrainTester : MonoBehaviour
{
    public string dem_fname = "DEM_sq";
    public string basmap_fname = "slope_grad";
    
    public float xres = 2.5f;
    public float yres = 2.5f;

    public void CreateTerrain()
    {
        var htmap = ProceduralTerrain.Read32BitTiff(dem_fname);
        var basemap = (Texture2D)Resources.Load(basmap_fname, typeof(Texture2D));
        Texture2D normal = ProceduralTerrain.GenerateNormalMap(htmap, xres, yres);

        ProceduralTerrain.BuildTerrain(htmap, xres, yres, basemap, normal);
    }

}
