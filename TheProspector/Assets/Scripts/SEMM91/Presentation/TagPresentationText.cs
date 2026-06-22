using SEMM91.Core.Tags;

namespace SEMM91.Presentation
{
    /// <summary>
    /// Converts internal tag coordinates into canonical
    /// player-facing terminology.
    ///
    /// Internal:
    /// Physical / Negative / Weak
    ///
    /// Player-facing:
    /// Malevolence (1)
    ///
    /// Descriptive:
    /// Spiteful
    /// </summary>
    public static class TagPresentationText
    {
        public static string FormatTag(
            TagAxis axis,
            TagPole pole,
            TagDegree degree)
        {
            if (!IsKnownDegree(degree))
                return "Unknown tag";

            string familyName =
                GetFamilyName(axis, pole);

            if (familyName == "Unknown")
                return "Unknown tag";

            return $"{familyName} ({(int)degree})";
        }

        public static string FormatDetailedTag(
            TagAxis axis,
            TagPole pole,
            TagDegree degree)
        {
            string tagName =
                FormatTag(axis, pole, degree);

            string descriptor =
                GetDegreeDescriptor(
                    axis,
                    pole,
                    degree
                );

            if (tagName == "Unknown tag" ||
                descriptor == "Unknown")
            {
                return "Unknown tag";
            }

            return $"{tagName} — {descriptor}";
        }

        public static string GetFamilyName(
            TagAxis axis,
            TagPole pole)
        {
            return (axis, pole) switch
            {
                (
                    TagAxis.Symbolic,
                    TagPole.Negative
                ) =>
                    "Profanity",

                (
                    TagAxis.Symbolic,
                    TagPole.Positive
                ) =>
                    "Sacredness",

                (
                    TagAxis.Emotional,
                    TagPole.Negative
                ) =>
                    "Coldness",

                (
                    TagAxis.Emotional,
                    TagPole.Positive
                ) =>
                    "Warmth",

                (
                    TagAxis.Expressive,
                    TagPole.Negative
                ) =>
                    "Rawness",

                (
                    TagAxis.Expressive,
                    TagPole.Positive
                ) =>
                    "Refinement",

                (
                    TagAxis.Temporal,
                    TagPole.Negative
                ) =>
                    "Speed",

                (
                    TagAxis.Temporal,
                    TagPole.Positive
                ) =>
                    "Slowness",

                (
                    TagAxis.Physical,
                    TagPole.Negative
                ) =>
                    "Malevolence",

                (
                    TagAxis.Physical,
                    TagPole.Positive
                ) =>
                    "Benevolence",

                (
                    TagAxis.Existential,
                    TagPole.Negative
                ) =>
                    "Morbidity",

                (
                    TagAxis.Existential,
                    TagPole.Positive
                ) =>
                    "Vitality",

                (
                    TagAxis.Interpretive,
                    TagPole.Negative
                ) =>
                    "Void",

                (
                    TagAxis.Interpretive,
                    TagPole.Positive
                ) =>
                    "Meaning",

                _ =>
                    "Unknown"
            };
        }

        public static string GetDegreeDescriptor(
            TagAxis axis,
            TagPole pole,
            TagDegree degree)
        {
            return (axis, pole) switch
            {
                (
                    TagAxis.Symbolic,
                    TagPole.Negative
                ) =>
                    SelectDegree(
                        degree,
                        "Irreverent",
                        "Defiant",
                        "Desecrating",
                        "Sacrilegious"
                    ),

                (
                    TagAxis.Symbolic,
                    TagPole.Positive
                ) =>
                    SelectDegree(
                        degree,
                        "Reverent",
                        "Devout",
                        "Consecrated",
                        "Sanctified"
                    ),

                (
                    TagAxis.Emotional,
                    TagPole.Negative
                ) =>
                    SelectDegree(
                        degree,
                        "Distant",
                        "Aloof",
                        "Bitter",
                        "Glacial"
                    ),

                (
                    TagAxis.Emotional,
                    TagPole.Positive
                ) =>
                    SelectDegree(
                        degree,
                        "Familial",
                        "Welcoming",
                        "Embracing",
                        "Fervent"
                    ),

                (
                    TagAxis.Expressive,
                    TagPole.Negative
                ) =>
                    SelectDegree(
                        degree,
                        "Rough",
                        "Unpolished",
                        "Primitive",
                        "Brutal"
                    ),

                (
                    TagAxis.Expressive,
                    TagPole.Positive
                ) =>
                    SelectDegree(
                        degree,
                        "Polished",
                        "Refined",
                        "Precise",
                        "Masterful"
                    ),

                (
                    TagAxis.Temporal,
                    TagPole.Negative
                ) =>
                    SelectDegree(
                        degree,
                        "Urgent",
                        "Driving",
                        "Frenetic",
                        "Blistering"
                    ),

                (
                    TagAxis.Temporal,
                    TagPole.Positive
                ) =>
                    SelectDegree(
                        degree,
                        "Measured",
                        "Heavy",
                        "Dragging",
                        "Monolithic"
                    ),

                (
                    TagAxis.Physical,
                    TagPole.Negative
                ) =>
                    SelectDegree(
                        degree,
                        "Irritable",
                        "Spiteful",
                        "Vindictive",
                        "Malicious"
                    ),

                (
                    TagAxis.Physical,
                    TagPole.Positive
                ) =>
                    SelectDegree(
                        degree,
                        "Kind",
                        "Generous",
                        "Protective",
                        "Compassionate"
                    ),

                (
                    TagAxis.Existential,
                    TagPole.Negative
                ) =>
                    SelectDegree(
                        degree,
                        "Gloomy",
                        "Macabre",
                        "Funereal",
                        "Sepulchral"
                    ),

                (
                    TagAxis.Existential,
                    TagPole.Positive
                ) =>
                    SelectDegree(
                        degree,
                        "Lively",
                        "Energetic",
                        "Thriving",
                        "Exuberant"
                    ),

                (
                    TagAxis.Interpretive,
                    TagPole.Negative
                ) =>
                    SelectDegree(
                        degree,
                        "Hollow",
                        "Nihilistic",
                        "Bleak",
                        "Abyssal"
                    ),

                (
                    TagAxis.Interpretive,
                    TagPole.Positive
                ) =>
                    SelectDegree(
                        degree,
                        "Purposeful",
                        "Devoted",
                        "Zealous",
                        "Transcendent"
                    ),

                _ =>
                    "Unknown"
            };
        }

        private static string SelectDegree(
            TagDegree degree,
            string degree0,
            string degree1,
            string degree2,
            string degree3)
        {
            return degree switch
            {
                TagDegree.Neutral =>
                    degree0,

                TagDegree.Weak =>
                    degree1,

                TagDegree.Dominant =>
                    degree2,

                TagDegree.Transgressive =>
                    degree3,

                _ =>
                    "Unknown"
            };
        }

        private static bool IsKnownDegree(
            TagDegree degree)
        {
            return degree is
                TagDegree.Neutral or
                TagDegree.Weak or
                TagDegree.Dominant or
                TagDegree.Transgressive;
        }
    }
}