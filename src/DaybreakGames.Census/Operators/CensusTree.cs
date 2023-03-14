using System;
using System.Collections.Generic;

namespace DaybreakGames.Census.Operators
{
    public sealed class CensusTree : CensusOperator
    {
        private string TreeFieldName { get; set; }
        private List<CensusTree> Tree { get; set; }

        [UriQueryProperty]
        private bool List { get; set; } = false;

        [UriQueryProperty]
        private string Prefix { get; set; }

        [UriQueryProperty]
        private string Start { get; set; }

        public CensusTree(string treeField)
        {
            TreeFieldName = treeField;
            Tree = new List<CensusTree>();
        }

        public CensusTree IsList(bool isList)
        {
            List = isList;
            return this;
        }

        public CensusTree GroupPrefix(string prefix)
        {
            Prefix = prefix;
            return this;
        }

        public CensusTree StartField(string field)
        {
            Start = field;
            return this;
        }

        public CensusTree TreeField(string field, Action<CensusTree> tree)
        {
            var newTree = new CensusTree(field);
            tree.Invoke(newTree);

            if (Tree == null)
            {
                Tree = new List<CensusTree>();
            }
            
            Tree.Add(newTree);

            return this;
        }

        public CensusTree TreeField(string field)
        {
            if (Tree == null)
            {
                Tree = new List<CensusTree>();
            }

            var newTree = new CensusTree(field);
            Tree.Add(newTree);
            return newTree;
        }

        public override string ToString()
        {
            var baseString = base.ToString();

            if (baseString.Length > 0)
            {
                baseString = $"^{baseString}";
            }

            var subJoinString = "";
            foreach (var subTree in Tree)
            {
                subJoinString += $"({subTree.ToString()})";
            }

            return $"{TreeFieldName}{baseString}{subJoinString}";
        }

        public override string GetKeyValueStringFormat()
        {
            return "{0}:{1}";
        }

        public override string GetPropertySpacer()
        {
            return "^";
        }

        public override string GetTermSpacer()
        {
            return "'";
        }
    }
}
