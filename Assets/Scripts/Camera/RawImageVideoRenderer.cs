/*
 * Unity Network Hardware Video Decoder
 * 
 * Copyright 2019 (C) Bartosz Meglicki <meglickib@gmail.com>
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 *
 */

using System;
using UnityEngine;
using UnityEngine.UI; //RawImage

public class RawImageVideoRenderer : MonoBehaviour
{
	public string device = "/dev/dri/renderD128";
	public string ip = "";
	public ushort port = 9766;

	private IntPtr nhvd;
	private NHVD.nhvd_frame frame = new NHVD.nhvd_frame{ data=new System.IntPtr[3], linesize=new int[3] };
	private Texture2D texture1, texture2, texture3;

	void Awake()
	{
		NHVD.nhvd_hw_config hw_config = new NHVD.nhvd_hw_config{hardware="vaapi", codec="h264", device=this.device, pixel_format="yuv420p"};
		NHVD.nhvd_net_config net_config = new NHVD.nhvd_net_config{ip=this.ip, port=this.port, timeout_ms=500 };

		nhvd=NHVD.nhvd_init (ref net_config, ref hw_config);

		if (nhvd == IntPtr.Zero)
		{
			Debug.Log ("failed to initialize NHVD");
			gameObject.SetActive (false);
		}
	}
	void OnDestroy()
	{
		NHVD.nhvd_close (nhvd);
	}

	private void AdaptTexture()
	{
		if(texture1== null || texture1.width != frame.width || texture1.height != frame.height)
        {
            //frame.format is AVPixelFormat
            if (frame.format == 0) //yuv420p
            {
                texture1 = new Texture2D(frame.width, frame.height, TextureFormat.R8, false);
                texture2 = new Texture2D(frame.width / 2, frame.height / 2, TextureFormat.R8, false);
                texture3 = new Texture2D(frame.width / 2, frame.height / 2, TextureFormat.R8, false);
                GetComponent<RawImage>().texture = texture1;
                GetComponent<RawImage>().material.SetTexture("_U", texture2);
                GetComponent<RawImage>().material.SetTexture("_V", texture3);
            }
            else if (frame.format == 25) //nv12
            {
                texture1 = new Texture2D(frame.width, frame.height, TextureFormat.R8, false);
                texture2 = new Texture2D(frame.width / 2, frame.height / 2, TextureFormat.RG16, false);
                GetComponent<RawImage>().texture = texture1;
                GetComponent<RawImage>().material.SetTexture("_UV", texture2);
            }
		}
	}

    private void FillTexture()
    {
        if (frame.format == 0) //yuv420p
        {
            texture1.LoadRawTextureData (frame.data[0], frame.width*frame.height);
            texture1.Apply (false);
            texture2.LoadRawTextureData (frame.data [1], frame.width * frame.height / 4);
            texture2.Apply (false);
            texture3.LoadRawTextureData (frame.data [2], frame.width * frame.height / 4);
            texture3.Apply (false); 
        }
        else if (frame.format == 25) //nv12
        {
            texture1.LoadRawTextureData (frame.data[0], frame.width*frame.height);
            texture1.Apply (false);
            texture2.LoadRawTextureData (frame.data [1], frame.width * frame.height / 2);
            texture2.Apply (false);
        }

    }
						
	// Update is called once per frame
	void LateUpdate ()
	{
		if (NHVD.nhvd_get_frame_begin(nhvd, ref frame) == 0)
		{
			AdaptTexture();
            FillTexture();
		}

		if (NHVD.nhvd_get_frame_end (nhvd) != 0)
			Debug.LogWarning ("Failed to get NHVD frame data");
	}
}