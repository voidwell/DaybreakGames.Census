using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace DaybreakGames.Census.Operators
{
    public sealed class CensusJoin : CensusOperator
    {
        [UriQueryProperty]
        private bool List { get; set; } = false;

        [DefaultValue(true)]
        [UriQueryProperty]
        private bool Outer { get; set; } = true;

        [UriQueryProperty]
        private IEnumerable<string> Show { get; set; }

        [UriQueryProperty]
        private IEnumerable<string> Hide { get; set; }

        [UriQueryProperty]
        private IEnumerable<CensusArgument> Terms { get; set; }

        [UriQueryProperty]
        private string On { get; set; }

        [UriQueryProperty]
        private string To { get; set; }

        [UriQueryProperty("inject_at")]
        private string InjectAt { get; set; }

        private List<CensusJoin> Join { get; set; } = new List<CensusJoin>();

        private string Service;

        public CensusJoin(string service)
        {
            Service = service;
        }

        public CensusJoin IsList(bool isList)
        {
            List = isList;
            return this;
        }

        public CensusJoin IsOuterJoin(bool isOuter)
        {
            Outer = isOuter;
            return this;
        }

        public CensusJoin ShowFields(params string[] fields)
        {
            Show = fields;
            return this;
        }

        public CensusJoin HideFields(params string[] fields)
        {
            Hide = fields;
            return this;
        }

        public CensusJoin OnField(string field)
        {
            On = field;
            return this;
        }

        public CensusJoin ToField(string field)
        {
            To = field;
            return this;
        }

        public CensusJoin WithInjectAt(string field)
        {
            InjectAt = field;
            return this;
        }

        public CensusJoin Where(string field, Action<CensusOperand> operand)
        {
            var arg = new CensusArgument(field);
            operand.Invoke(arg.Operand);

            if (Terms == null)
            {
                Terms = new List<CensusArgument>();
            }

            var terms = Terms as List<CensusArgument>;
            terms.Add(arg);
            Terms = terms;

            return this;
        }

        public CensusOperand Where(string field)
        {
            var arg = new CensusArgument(field);

            if (Terms == null)
            {
                Terms = new List<CensusArgument>();
            }

            var terms = Terms as List<CensusArgument>;
            terms.Add(arg);
            Terms = terms;

            return arg.Operand;
        }

        public CensusJoin JoinService(string service, Action<CensusJoin> join)
        {
            var newJoin = new CensusJoin(service);
            join.Invoke(newJoin);

            Join.Add(newJoin);
            return this;
        }

        public CensusJoin JoinService(string service)
        {
            var newJoin = new CensusJoin(service);
            Join.Add(newJoin);
            return newJoin;
        }

        public override string ToString()
        {
            var baseString = base.ToString();

            if (baseString.Length > 0)
            {
                baseString = $"^{baseString}";
            }

            var subJoinString = "";
            foreach (var subJoin in Join)
            {
                subJoinString += $"({subJoin.ToString()})";
            }

            return $"{Service}{baseString}{subJoinString}";
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
