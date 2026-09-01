using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using _01.Scripts.Manager;
using UnityEngine;

public enum ShopConditionType
{
	Cash,
	Ads
}

public enum ShopRewardType
{
	Block,
	Money,
	Luck,
	NoAds
}

public class ShopData
{
	[XmlAttribute]
	public int ID;
	[XmlAttribute]
	public int name;
	[XmlAttribute]
	public ShopConditionType condition;
	[XmlAttribute]
	public int price; // ������ ���� ���
	[XmlAttribute]
	public string productID; // ������ ���� ���
	[XmlAttribute]
	public ShopRewardType rewardType;
	[XmlAttribute]
	public int rewardCount;
	[XmlAttribute]
	public string icon;
}

[Serializable, XmlRoot("ArrayOfShopData")]
public class ShopDataLoader : ILoader<int, ShopData>
{
	[XmlElement("ShopData")]
	public List<ShopData> shopDatas = new();

	public Dictionary<int, ShopData> MakeDic()
	{
		Dictionary<int, ShopData> dic = new Dictionary<int, ShopData>();

		foreach (ShopData data in shopDatas)
			dic.Add(data.ID, data);

		return dic;
	}

	public bool Validate()
	{
		return true;
	}
}