﻿using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

[ExecuteInEditMode]
public class GenerateTextureNumberTest : MonoBehaviour
{
	bool runNow;
	bool extraRun = true;
	private int[,] initColorMap;
	private int width = 2049; //These 2 defined by input! Each terrain 4097 pixels wide and long
	private int length; //Input is amount of tiles in width and length (Ex: 2x3 tiles)
	private float[,] finalHeightMap; //defines the elevation of each height point between 0.0 and 1.0
	private int terrainWidth = 14000; //defines the width of the terrain in meters
	private int terrainHeight = 2000; //defines the maximum possible height of the terrain
	private int terrainLength = 10000; //defines the length of the terrain in meters
	private int[,] colorMap;
	private Texture2D tex;
	private float[, ] pixelDistances;
	
	//important note:
	//boundary of map defined by:
	//!((k+y) < 0 || (k + y) > (length-1) || (z + x) < 0 || (z + x) > (width-1))
	
	enum ground : int
	{
		Field,
		Mountain,
		Water,
		City 
	};
	
	// Use this for initialization
	void Start ()
	{
		length = width;
		runNow = true;

		colorMap = new int[width, length];
		
		pixelDistances = new float[width, length];

		tex = Resources.Load("InputPictureG") as Texture2D;
	}
	
	// Update is called once per frame
	void Update ()
	{
		if (runNow && extraRun) {
			convertInputIntoMap ();
		}
	}
	
	void convertInputIntoMap ()
	{
		print ("Start running processor melting program.");
		
		setColors ();

		setDistances ();

		//create matrix of floats, set to the integer matrix where the minimum
		//integer value is normalized to 0.0f and the maximum value is at 1.0f
		createFloatMatrix ();
		
		//Create terrain and send it through the world
		createTerrain ();
		
		runNow = false;
		extraRun = false;
	}
	
	void setColors ()
	{
		//GetPixel is not efficient. This method could run 100X faster if I replace that with GetPixels or GetPixels32 or whatever I need.
		
		int imageLoopX = tex.width;
		int imageLoopY = tex.height;
		
		int loopX = 0;
		int loopY = 0;
		
		int xPlaced = width / imageLoopX;
		int yPlaced = length / imageLoopY;
		
		int placeX = 0;
		int placeY = 0;
		
		print ("Values:  " + imageLoopX +  "  " + imageLoopY + "  " + loopX +  "  " + loopY + "  ");
		print ("Values:  " + xPlaced +  "  " + yPlaced + "  " + placeX +  "  " + placeY + "  ");
		
		while (loopY < imageLoopY) {
			while (loopX < imageLoopX) {
				while(placeY < yPlaced){
					while(placeX < xPlaced){
						if((yPlaced*loopY)+placeY < length && (xPlaced*loopX)+placeX < width){
							
							if(tex.GetPixel(loopX, loopY).g > 0.5)
							{ //field
								colorMap[(yPlaced*loopY)+placeY, (xPlaced*loopX)+placeX] = (int) ground.Field;
								
							}
							else if(tex.GetPixel(loopX, loopY).r > 0.7)
							{ //mountains
								colorMap[(yPlaced*loopY)+placeY, (xPlaced*loopX)+placeX] = (int) ground.Mountain;
								
							}
							else if(tex.GetPixel(loopX, loopY).b > 0.7)
							{ //water 
								colorMap[(yPlaced*loopY)+placeY, (xPlaced*loopX)+placeX] = (int) ground.Water;
								
							}
							else
							{ //city
								colorMap[(yPlaced*loopY)+placeY, (xPlaced*loopX)+placeX] = (int) ground.City;
							}
						}
						placeX++;
					}
					placeX=0;
					placeY++;
				}
				placeY=0;
				loopX++;
			}
			loopX=0;
			loopY++;
		}
	}

	private void setDistances ()
	{
		for(int y = 0; y < pixelDistances.GetLength(0); y++)
		{
			for(int x = 0; x < pixelDistances.GetLength(1); x++)
			{
				if(colorMap[y, x]== (int) ground.Mountain){
					if(y==0 || x==0){
						pixelDistances[y, x] = 1;
					}
					else if(colorMap[y, x-1] == (int) ground.Mountain){
						pixelDistances[y, x] = pixelDistances[y, x-1]+1;
					}
					else{
						pixelDistances[y, x] = 1;
					}
				}
			}
		}

		for(int y = pixelDistances.GetLength(0)-1; y >= 0; y--)
		{
			for(int x = pixelDistances.GetLength(1)-1; x >= 0; x--)
			{
				if(colorMap[y, x]== (int) ground.Mountain){
					if(y == pixelDistances.GetLength(0)-1 || x == pixelDistances.GetLength(1)-1){
						pixelDistances[y, x] = 1;
					}
					else if(colorMap[y, x+1] == (int) ground.Mountain){
						if(pixelDistances[y, x] >= pixelDistances[y, x+1])
							pixelDistances[y, x] = pixelDistances[y, x+1]+1;
					}
					else{
						pixelDistances[y, x] = 1;
					}
				}
			}
		}
	}
	
	/// <summary>
	/// ######################
	/// ######################
	/// BEGIN SECTION DEVOTED TO NOISE
	/// ######################
	/// ######################
	/// </summary>
	
	private void createFloatMatrix ()
	{
		
		finalHeightMap = new float[length, width];
		
		for (int y = 0; y < length-1; y++) {
			
			for (int x = 0; x < width-1; x++) {
				
				if(colorMap[y, x] == (int) ground.Field){ //field
					finalHeightMap[y, x] = 0.1f;
					
				}else if(colorMap[y, x] == (int) ground.Mountain){ //mountains
					finalHeightMap[y, x] = 0.1f + pixelDistances[y, x]*0.02f;
					
				}else if(colorMap[y, x] == (int) ground.Water){ //water 
					finalHeightMap[y, x] = 0.0f;
				}
				else{ //city
					finalHeightMap[y, x] = 0.1f;
				}
			}
		}
		setMin ();
	}

	private void setMin (){
		float min = 1f;
		float max = 0f;
		length = width = finalHeightMap.GetLength(0);
		
		for (int y = 0; y < length-1; y++) {
			for (int x = 0; x < width-1; x++) {
				if(finalHeightMap [y, x] < min){
					min = finalHeightMap [y, x];
				}
				if(finalHeightMap [y, x] > max){
					max = finalHeightMap [y, x];
				}
			}
		}
		
		min = Math.Abs (min); 

		terrainHeight = (int)(max*500f);

		for (int y = 0; y < length-1; y++) {
			for (int x = 0; x < width-1; x++) {
				finalHeightMap [y, x] = (finalHeightMap [y, x] + min) / (max + min);
			}
		}
	}
	
	private void createTerrain ()
	{
		TerrainData terrainData = new TerrainData ();
		
		terrainData.heightmapResolution = width;
		terrainData.baseMapResolution = 1024;
		terrainData.SetDetailResolution (1024, 16);
		
		terrainData.SetHeights (0, 0, finalHeightMap);
		terrainData.size = new Vector3 (terrainWidth, terrainHeight, terrainLength);
		//terrainData.splatPrototypes = m_splatPrototypes;
		//terrainData.treePrototypes = m_treeProtoTypes;
		//terrainData.detailPrototypes = m_detailProtoTypes;
		GameObject go = Terrain.CreateTerrainGameObject (terrainData);
		go.transform.position.Set (0, 0, 0);
		print ("It made it to the end");
	}
	
	//inputs A and B are the numbers that are being interpolated between.
	//X is the fraction of difference between A and B.
	private float smoothInterpolate (float a, float b, float x)
	{
		float ft = x * 3.1415927f;
		float f = (float)(1 - Math.Cos (ft)) * 0.5f;
		
		return  (float)(a * (1 - f) + b * f);
	}
	
	private float interpolate(float a, float b, float x){
		return a*(1f-x) + b*x;
	}
}
