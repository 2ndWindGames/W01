using System.Xml.Serialization;

namespace SWGUnity2DCore.Data
{
	public class SalaryNegotiationData
	{
		[XmlAttribute]
		public int questionID;
		[XmlAttribute]
		public int yesAnswerID;
		[XmlAttribute]
		public int noAnswerID;
		[XmlAttribute]
		public int yesResultID;
		[XmlAttribute]
		public int noResultGoodID;
		[XmlAttribute]
		public int noResultBadID;
		[XmlAttribute]
		public int yesIncreaseSalaryPercent;
		[XmlAttribute]
		public int noIncreaseSalaryPercentGood;
		[XmlAttribute]
		public int noIncreaseSalaryPercentBad;
	}
}