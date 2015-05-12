/*
 * Copyright (c) 2014, Roger Lew (rogerlew.gmail.com)
 * Date: 5/12/2015
 * License: BSD (3-clause license)
 * 
 * The project described was supported by NSF award number IIA-1301792
 * from the NSF Idaho EPSCoR Program and by the National Science Foundation.
 * 
 */

using UnityEngine;
using System.Collections;

namespace VTL.ProceduralTerrain
{
    public static class ProceduralTerrain
    {
        public static float[,] Read32BitTiff(string fname, bool littleEndian=false)
        {
            // read the data out of the texture
            Texture2D tex = Resources.Load<Texture2D>(fname);
            Color[] pixelData = tex.GetPixels();

            float[,] htmap = new float[tex.width, tex.height];
            byte[] byteData = new byte[4];

            int k = 0;
            for (int i = 0; i < tex.width; i++)
            {
                for (int j = 0; j < tex.width; j++)
                {
                    if (littleEndian)
                    {
                        byteData[0] = (byte)(pixelData[k].a * 255f);
                        byteData[1] = (byte)(pixelData[k].r * 255f);
                        byteData[2] = (byte)(pixelData[k].g * 255f);
                        byteData[3] = (byte)(pixelData[k].b * 255f);
                    }
                    else
                    {
                        byteData[3] = (byte)(pixelData[k].a * 255f);
                        byteData[2] = (byte)(pixelData[k].r * 255f);
                        byteData[1] = (byte)(pixelData[k].g * 255f);
                        byteData[0] = (byte)(pixelData[k].b * 255f);
                    }

                    htmap[i, j] = System.BitConverter.ToSingle(byteData, 0);
                    k++;
                }
            }
            return htmap;
        }

        // <summary>Generates and retures a normal map from a 2d array of floats (vals).
        // <para>xres and yres represent the distance between pixels in the first and second
        // axes of the vals array</para>
        // <para>The Terrain is returned as a Gameobject</para>
        // </summary>
        public static GameObject BuildTerrain(float[,] htmap, float xres, float yres, 
                                              Texture2D basemap, Texture2D normal=null,
                                              int detailResolution=128,
                                              int resolutionPerPatch = 8)
        {
            int nx = htmap.GetLength(0);
            int ny = htmap.GetLength(1);

            if (nx != ny)
                throw new System.Exception("htmap must be square");

            if (!Mathf.IsPowerOfTwo(nx - 1))
                throw new System.Exception("htmap size must be power of two + 2");

            if (basemap == null)
                Debug.LogWarning("Basemap is null");

            float ymin, ymax, yrange;
            CalculateMinMaxRange2dArray(htmap, out ymin, out ymax, out yrange);

            // Unity wants terrain data normalized between 0 and 1
            // htmap is normalized in-place
            Normalize2dArray(ref htmap, ymin, ymax, yrange);

            TerrainData terrainData = new TerrainData();
            terrainData.heightmapResolution = nx;
            terrainData.alphamapResolution = nx - 1;
            terrainData.SetDetailResolution(detailResolution, resolutionPerPatch);
            terrainData.SetHeights(0, 0, htmap);

            float width = xres * nx;
            float length = yres * ny;
            terrainData.size = new Vector3(width, yrange, length);

            SplatPrototype[] splatPrototypes = new SplatPrototype[1];
            splatPrototypes[0] = new SplatPrototype();
            splatPrototypes[0].normalMap = normal;
            splatPrototypes[0].texture = basemap;
            splatPrototypes[0].tileOffset = new Vector2(0, 0);
            splatPrototypes[0].tileSize = new Vector2(width, length);

            terrainData.splatPrototypes = splatPrototypes;

            GameObject terrain;
            terrain = Terrain.CreateTerrainGameObject(terrainData);

            return terrain;
        }


        // <summary>Return the gradient of a 2d array of floats
        //
        // <para>The gradient is computed using second order accurate central differences
        // in the interior and either first differences or second order accurate 
        // one-sides (forward or backwards) differences at the boundaries. The
        // returned gradient hence has the same shape as the input array.</para>
        //
        // <para>Ported from numpy:
        // https://github.com/numpy/numpy/blob/v1.9.1/numpy/lib/function_base.py#L886
        // </para>
        // </summary>
        public static void Gradient2dArray(float[,] vals, out float[,] dx, out float[,] dy)
        {

            int nx = vals.GetLength(0);
            int ny = vals.GetLength(1);
            dx = new float[nx, ny];
            dy = new float[nx, ny];
            
            for (int i = 1; i < nx-1; i++)
                for (int j = 0; j < ny; j++)
                    dx[i, j] = (vals[i + 1, j] - vals[i - 1, j]) / 2;
            
            for (int i = 0; i < nx; i++)
            {
                dy[i, 0] = vals[i, 1] - vals[i, 0];
                dy[i, ny-1] = vals[i, ny-1] - vals[i, ny-2];
            }
            
            for (int j = 0; j < ny; j++)
            {
                dx[0, j] = vals[1, j] - vals[0, j];
                dx[nx-1, j] = vals[nx-1, j] - vals[nx-2, j];
            }

            for (int i = 0; i < nx; i++)
                for (int j = 1; j < ny - 1; j++)
                    dy[i, j] = (vals[i, j + 1] - vals[i, j - 1]) / 2;
        }

        // <summary>Given the “legs” of a right triangle, return its hypotenuse.
        //
        // <para>Equivalent to sqrt(x1**2 + x2**2), element-wise. If x1 or x2 is scalar_like (i.e., 
        // unambiguously cast-able to a scalar type), it is broadcast for use with each element 
        // of the other argument. (See Examples) </para>
        // <para>xres and yres represent the distance between pixels in the first and second
        // axes of the vals array</para>
        // </summary>
        public static float Hypot(float x1, float x2)
        {
            return Mathf.Sqrt(Mathf.Pow(x1, 2) + Mathf.Pow(x2, 2));
        }

        // <summary>Calculates the surface normals of a 2d array of floats representing a surface.
        // </summary>
        public static void SurfaceNormals(float[,] vals, float xres, float yres, 
                                          out float[,] dx, out float[,] dy, out float[,] dz)
        {
            int nx = vals.GetLength(0);
            int ny = vals.GetLength(1);
            dz = new float[nx, ny];

            Gradient2dArray(vals, out dx, out dy);
            
            for (int i = 0; i < nx; i++)
            {
                for (int j = 0; j < ny; j++)
                {
                    dx[i,j] /= -xres;
                    dy[i,j] /= -yres;
            
                    float slope = Mathf.Atan(Hypot(dx[i,j], dy[i,j]));
                    dz[i,j] = Mathf.Cos(slope);

                    float d = Mathf.Sqrt(Mathf.Pow(dx[i,j], 2) + 
                                         Mathf.Pow(dy[i,j], 2) + 
                                         Mathf.Pow(dz[i,j], 2));

                    dx[i, j] /= d;
                    dy[i, j] /= d;
                    dz[i, j] /= d;
                }
            }
        }

        // <summary>Generates and retures a normal map from a 2d array of floats (vals).
        // <para>xres and yres represent the distance between pixels in the first and second
        // axes of the vals array</para>
        // </summary>
        public static Texture2D GenerateNormalMap(float[,] vals, float xres, float yres)
        {
            int nx = vals.GetLength(0);
            int ny = vals.GetLength(1);

            float[,] dx, dy, dz;
            SurfaceNormals(vals, xres, yres, out dx, out dy, out dz);

            var rgb = new Color[(nx-1) * (ny-1)];

            int k = 0;
            for (int i = 0; i < nx - 1; i++)
            {
                for (int j = 0; j < ny - 1; j++)
                {
                    var r = Mathf.Clamp01(0.5f * dx[i, j] + 0.5f);
                    var g = Mathf.Clamp01(0.5f * dy[i, j] + 0.5f);
                    var b = Mathf.Clamp01(0.5f * dz[i, j] + 0.5f);
                    rgb[k++] = new Color(r, g, b);

                }
            }

            Texture2D normal = new Texture2D(nx-1, ny-1);
            normal.SetPixels(rgb);
            normal.Apply();

            return normal;
        }

        public static void CalculateMinMaxRange2dArray(float[,] data, out float min, 
                                                       out float max, out float range)
        {
            min = 1e38f;
            max = -1e38f;

            for (int i = 0; i < data.GetLength(0); i++)
            {
                for (int j = 0; j < data.GetLength(1); j++)
                {
                    if (data[i, j] < min)
                        min = data[i, j];

                    if (data[i, j] > max)
                        max = data[i, j];
                }
            }
            range = max - min;
        }

        public static void Normalize2dArray(ref float[,] data, float min, float max, float range)
        {
            // Unity wants terrain data normalized between 0 and 1
            for (int i = 0; i < data.GetLength(0); i++)
                for (int j = 0; j < data.GetLength(1); j++)
                    data[i, j] = (data[i, j] - min) / range;
        }
    }
}