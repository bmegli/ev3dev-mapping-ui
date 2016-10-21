/*
 * Copyright (C) 2016 Bartosz Meglicki <meglickib@gmail.com>
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License version 3 as
 * published by the Free Software Foundation.
 * This program is distributed "as is" WITHOUT ANY WARRANTY of any
 * kind, whether express or implied; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 */

using UnityEngine;
using System;
using System.Collections;
using UnityEngine.UI;

public class WifiUI : ModuleUI
{
	public Slider SignalSlider;

	private WifiSignalProperties signal;
	private Slider signalSlider;
	private Image signalFill;

	private Text ppsText;
	private Text ssidText;
	private Text bssidText;
	private Text rxTxText;

	private Wifi wifi;

	protected override void Awake()
	{
		base.Awake();
		ppsText = SafeInstantiateText(ModuleText, uiTransform, "pps 00 ms 00");
		ssidText = SafeInstantiateText(ModuleText, uiTransform, "ssid");
		bssidText = SafeInstantiateText(ModuleText, uiTransform, "bssid");
		rxTxText = SafeInstantiateText(ModuleText, uiTransform, "rx 0 tx 0");
		signalSlider = SafeInstantiate<Slider>(SignalSlider, uiTransform);
		signalFill = signalSlider.fillRect.GetComponentInChildren<Image>();
		RectTransform r = signalFill.GetComponent<RectTransform>();
		r.offsetMax = r.offsetMin = new Vector2(0, 0);
	}

	protected override void Start ()
	{
		base.Start();
		wifi = module as Wifi;
		signal = wifi.signal;
		signalSlider.minValue = signal.minValueDbm;
		signalSlider.maxValue = signal.maxValueDbm;
		signalSlider.value = signal.minValueDbm;
	

	}

	private void SetSignalSliderFillColor(int signalDbm)
	{
		if (signalDbm > signal.warningLevelDbm)
			signalFill.color = Color.green;
		else if (signalDbm > signal.criticalLevelDbm)
			signalFill.color = Color.yellow;
		else
			signalFill.color = Color.red;	
	}

	protected override void Update ()
	{
		base.Update();
		float avgPacketMs = wifi.GetAveragedPacketTimeMs();

		if (avgPacketMs != 0)
			ppsText.text = string.Format("pps {0:00} ms {1:00}", 1000.0f / avgPacketMs, avgPacketMs);

		ssidText.text = string.Format("{0} {1:+00;-00} dBm",wifi.GetSSID(), wifi.GetSignalDbm()); 
		bssidText.text = wifi.GetBSSID();
		//rxTxText.text=string.Format("rx {0:00000} tx {1:00000}", wifi.GetRxPackets(), wifi.GetTxPackets());
		rxTxText.text=string.Format("rx {0,5} tx {1,5}", wifi.GetRxPackets(), wifi.GetTxPackets());


		int signalDbm = wifi.GetSignalDbm();
		signalSlider.value = signalDbm;
		SetSignalSliderFillColor(signalDbm);
	}
}
