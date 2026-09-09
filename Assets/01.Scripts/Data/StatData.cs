using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using SWGUnity2DCore.Manager;

namespace SWGUnity2DCore.Data
{
	public class StatData
	{
		[XmlAttribute]
		public int ID;
		// [XmlAttribute]
		// public StatType type;
		[XmlAttribute]
		public int nameID;
		[XmlAttribute]
		public int price; // ���׷��̵� ���
		[XmlAttribute]
		public int increaseStat; // ���׷��̵� ��ȭ��
		[XmlAttribute]
		public int difMaxHp;
		[XmlAttribute]
		public int difAtk;
		[XmlAttribute]
		public float difAllAtkPercent;
		[XmlAttribute]
		public float difSalaryPercent;
		[XmlAttribute]
		public float difRevenuePercent;
		[XmlAttribute]
		public float cooltimePercent;
		[XmlAttribute]
		public float successPercent;
	}

	[Serializable, XmlRoot("ArrayOfStatData")]
	public class StatDataLoader : ILoader<int, StatData>
	{
		[XmlElement("StatData")]
		public List<StatData> _statData = new List<StatData>();

		public Dictionary<int, StatData> MakeDic()
		{
			Dictionary<int, StatData> dic = new Dictionary<int, StatData>();

			foreach (StatData data in _statData)
				dic.Add(data.ID, data);

			return dic;
		}

		public bool Validate()
		{
			return true;
		}
	}
}