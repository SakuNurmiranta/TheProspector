namespace SEMM91.Core.Tags
{

        public enum TagAxis
        {
            Symbolic,       //Profane<-0->Sacred
            Emotional,      //Cold<-0->Warm
            Expressive,     //Raw<-0->Honed
            Temporal,       //Fast<-0->Slow
            Physical,       //Malevolent <-0-> Benevolent
            Existential,    //Morbid <-0-> Vital
            Interpretive    //Void <-0-> Meaning
        }

        public enum TagPole
        {
            Negative,
            Positive
        }

        public enum TagDegree
        {
            Neutral = 0,
            Weak = 1,
            Dominant = 2,
            Transgressive = 3
        }
}
