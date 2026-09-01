using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using _01.Scripts.Manager;

namespace _01.Scripts.Data
{
    public class BlockEventExcelData
    {
        [XmlAttribute]
        public int enemyID;
        [XmlAttribute]
        public int answerID;
        [XmlAttribute]
        public int success;
        [XmlAttribute]
        public int resultID;
        [XmlAttribute]
        public int isDead;
        [XmlAttribute]
        public int difWorkAbility;
        [XmlAttribute]
        public int difLikability;
        [XmlAttribute]
        public int difLuck;
        [XmlAttribute]
        public int difBlock;

    }

    public class BlockEventData
    {
        [XmlAttribute]
        public int enemyID;
        [XmlArray]
        public List<BlockEventAnsData> ansData = new();
    }

    public class BlockEventAnsData
    {
        [XmlAttribute]
        public int enemyID;
        [XmlAttribute]
        public int answerID;
        [XmlAttribute]
        public int success;
        [XmlAttribute]
        public int resultID;
        [XmlAttribute]
        public int isDead;
        [XmlAttribute]
        public int difWorkAbility;
        [XmlAttribute]
        public int difLikability;
        [XmlAttribute]
        public int difLuck;
        [XmlAttribute]
        public int difBlock;
    }

    [Serializable, XmlRoot("ArrayOfBlockEventData")]
    public class BlockEventDataLoader : ILoader<int, BlockEventData>
    {
        [XmlElement("BlockEventData")]
        public List<BlockEventData> blockEventData = new List<BlockEventData>();

        public Dictionary<int, BlockEventData> MakeDic()
        {
            return blockEventData.ToDictionary(data => data.enemyID);
        }

        public bool Validate()
        {
            return true;
        }
    }
}

