using SEMM91.Core.Tags;

namespace SEMM91.Core.Tracks
{
    /// <summary>
    /// Canonical authored Track naming lexicon data.
    /// </summary>
    public static class TrackNamingLexiconCatalogData
    {
        public static TrackNamingLexiconCatalog CreateDefault()
        {
            return new TrackNamingLexiconCatalog(
                new[]
                {
                    CreateProfane(),
                    CreateSacred(),
                    CreateCold(),
                    CreateWarm(),
                    CreateRaw(),
                    CreateHoned(),
                    CreateFast(),
                    CreateSlow(),
                    CreateMalevolent(),
                    CreateBenevolent(),
                    CreateMorbid(),
                    CreateVital(),
                    CreateVoid(),
                    CreateMeaning(),
                }
            );
        }

        private static TrackNamingTagLexicon CreateProfane()
        {
            return new TrackNamingTagLexicon(
                TagAxis.Symbolic,
                TagPole.Negative,
                new[]
                {
                    Degree(
                        TagDegree.Neutral,
                        "Irreverent",
                        new[]
                        {
                            "irreverent",
                            "improper",
                            "vulgar",
                            "impious",
                        },
                        new[]
                        {
                            "mockery",
                            "grime",
                            "insult",
                            "filth",
                        },
                        new[]
                        {
                            "mocking",
                            "sneering at",
                            "scoffing at",
                            "smearing",
                        },
                        new[]
                        {
                            "the unclean",
                            "the gutter",
                            "the vulgar mouth",
                            "an idle insult",
                        }
                    ),
                    Degree(
                        TagDegree.Weak,
                        "Defiant",
                        new[]
                        {
                            "defiant",
                            "insolent",
                            "blasphemous",
                            "contemptuous",
                        },
                        new[]
                        {
                            "spit",
                            "curse",
                            "obscenity",
                            "defiance",
                        },
                        new[]
                        {
                            "spitting on",
                            "cursing",
                            "heckling",
                            "rejecting",
                        },
                        new[]
                        {
                            "the blasphemer",
                            "the profane sign",
                            "the defiant tongue",
                            "forbidden speech",
                        }
                    ),
                    Degree(
                        TagDegree.Dominant,
                        "Desecrating",
                        new[]
                        {
                            "desecrating",
                            "fouled",
                            "polluted",
                            "defiled",
                        },
                        new[]
                        {
                            "desecration",
                            "excrement",
                            "contamination",
                            "defilement",
                        },
                        new[]
                        {
                            "defiling",
                            "fouling",
                            "soiling",
                            "corrupting",
                        },
                        new[]
                        {
                            "desecrated ground",
                            "polluted flesh",
                            "the fouled image",
                            "the ruined taboo",
                        }
                    ),
                    Degree(
                        TagDegree.Transgressive,
                        "Sacrilegious",
                        new[]
                        {
                            "sacrilegious",
                            "unholy",
                            "violated",
                            "accursed",
                        },
                        new[]
                        {
                            "sacrilege",
                            "abomination",
                            "violation",
                            "damnation",
                        },
                        new[]
                        {
                            "violating",
                            "unmaking",
                            "annihilating",
                            "rendering unholy",
                        },
                        new[]
                        {
                            "the abomination",
                            "the broken taboo",
                            "the unholy throne",
                            "absolute profanity",
                        }
                    ),
                },
                new[]
                {
                    Group(
                        "Substances and instruments",
                        new[]
                        {
                            "dirt",
                            "spit",
                            "ash",
                            "excrement",
                            "blood",
                            "rot",
                            "blasphemy",
                            "the unclean hand",
                        }
                    ),
                    Group(
                        "Settings",
                        new[]
                        {
                            "in the gutter",
                            "before the congregation",
                            "upon consecrated ground",
                            "beneath the altar",
                            "mid-sermon",
                            "during prayer",
                            "at the chapel door",
                            "under the sacred image",
                        }
                    ),
                    Group(
                        "Consequences",
                        new[]
                        {
                            "left unclean",
                            "rendered profane",
                            "dragged through filth",
                            "stripped of holiness",
                            "reduced to mockery",
                            "made obscene",
                            "broken beneath contempt",
                        }
                    ),
                }
            );
        }

        private static TrackNamingTagLexicon CreateSacred()
        {
            return new TrackNamingTagLexicon(
                TagAxis.Symbolic,
                TagPole.Positive,
                new[]
                {
                    Degree(
                        TagDegree.Neutral,
                        "Reverent",
                        new[]
                        {
                            "reverent",
                            "solemn",
                            "prayerful",
                            "ceremonial",
                        },
                        new[]
                        {
                            "reverence",
                            "prayer",
                            "rite",
                            "offering",
                        },
                        new[]
                        {
                            "bowing before",
                            "invoking",
                            "honouring",
                            "blessing",
                        },
                        new[]
                        {
                            "the shrine",
                            "the prayer",
                            "the sacred face",
                            "the quiet rite",
                        }
                    ),
                    Degree(
                        TagDegree.Weak,
                        "Devout",
                        new[]
                        {
                            "devout",
                            "pious",
                            "devotional",
                            "faithful",
                        },
                        new[]
                        {
                            "devotion",
                            "sermon",
                            "liturgy",
                            "faith",
                        },
                        new[]
                        {
                            "venerating",
                            "preaching",
                            "blessing",
                            "dedicating",
                        },
                        new[]
                        {
                            "the altar",
                            "the chapel",
                            "the congregation",
                            "Sunday prayer",
                        }
                    ),
                    Degree(
                        TagDegree.Dominant,
                        "Consecrated",
                        new[]
                        {
                            "consecrated",
                            "holy",
                            "ordained",
                            "anointed",
                        },
                        new[]
                        {
                            "consecration",
                            "sacrament",
                            "reliquary",
                            "holy ground",
                        },
                        new[]
                        {
                            "anointing",
                            "consecrating",
                            "enthroning",
                            "ordaining",
                        },
                        new[]
                        {
                            "the consecrated altar",
                            "the holy relic",
                            "the ordained priest",
                            "sanctified ground",
                        }
                    ),
                    Degree(
                        TagDegree.Transgressive,
                        "Sanctified",
                        new[]
                        {
                            "sanctified",
                            "divine",
                            "eternal",
                            "transcendent",
                        },
                        new[]
                        {
                            "sanctity",
                            "revelation",
                            "divine law",
                            "apotheosis",
                        },
                        new[]
                        {
                            "sanctifying",
                            "canonizing",
                            "transfiguring",
                            "exalting",
                        },
                        new[]
                        {
                            "the final liturgy",
                            "the throne of heaven",
                            "the eternal commandment",
                            "the divine image",
                        }
                    ),
                },
                new[]
                {
                    Group(
                        "Persons and bodies",
                        new[]
                        {
                            "the priest",
                            "the believer",
                            "the congregation",
                            "the sacred face",
                            "the anointed hand",
                            "the devotional voice",
                        }
                    ),
                    Group(
                        "Objects and locations",
                        new[]
                        {
                            "altar",
                            "chapel",
                            "shrine",
                            "relic",
                            "icon",
                            "scripture",
                            "consecrated ground",
                            "temple",
                            "reliquary",
                        }
                    ),
                    Group(
                        "Ritual and temporal settings",
                        new[]
                        {
                            "during prayer",
                            "mid-sermon",
                            "on Sunday",
                            "before the congregation",
                            "within the liturgy",
                            "beneath the chapel bell",
                            "at the final blessing",
                            "during consecration",
                        }
                    ),
                    Group(
                        "Consequences",
                        new[]
                        {
                            "made holy",
                            "raised into devotion",
                            "returned to the altar",
                            "preserved as doctrine",
                            "sealed by faith",
                            "rendered untouchable",
                            "enthroned above doubt",
                        }
                    ),
                }
            );
        }

        private static TrackNamingTagLexicon CreateCold()
        {
            return new TrackNamingTagLexicon(
                TagAxis.Emotional,
                TagPole.Negative,
                new[]
                {
                    Degree(
                        TagDegree.Neutral,
                        "Distant",
                        new[]
                        {
                            "distant",
                            "remote",
                            "absent",
                            "withdrawn",
                        },
                        new[]
                        {
                            "distance",
                            "absence",
                            "silence",
                            "separation",
                        },
                        new[]
                        {
                            "turning from",
                            "overlooking",
                            "leaving",
                            "withdrawing from",
                        },
                        new[]
                        {
                            "the distant gaze",
                            "the empty chair",
                            "the unanswered voice",
                            "the closed room",
                        }
                    ),
                    Degree(
                        TagDegree.Weak,
                        "Aloof",
                        new[]
                        {
                            "aloof",
                            "detached",
                            "indifferent",
                            "reserved",
                        },
                        new[]
                        {
                            "indifference",
                            "refusal",
                            "reserve",
                            "disregard",
                        },
                        new[]
                        {
                            "withholding",
                            "refusing",
                            "dismissing",
                            "passing by",
                        },
                        new[]
                        {
                            "the offered hand",
                            "the familiar voice",
                            "the open door",
                            "the waiting face",
                        }
                    ),
                    Degree(
                        TagDegree.Dominant,
                        "Bitter",
                        new[]
                        {
                            "bitter",
                            "estranged",
                            "hardened",
                            "resentful",
                        },
                        new[]
                        {
                            "bitterness",
                            "grievance",
                            "resentment",
                            "frost",
                        },
                        new[]
                        {
                            "severing",
                            "scorning",
                            "freezing",
                            "poisoning",
                        },
                        new[]
                        {
                            "the bond",
                            "the embrace",
                            "the shared memory",
                            "the family hearth",
                        }
                    ),
                    Degree(
                        TagDegree.Transgressive,
                        "Glacial",
                        new[]
                        {
                            "glacial",
                            "merciless",
                            "frozen",
                            "absolute",
                        },
                        new[]
                        {
                            "isolation",
                            "permafrost",
                            "white silence",
                            "emotional extinction",
                        },
                        new[]
                        {
                            "erasing",
                            "extinguishing",
                            "entombing",
                            "freezing beyond return",
                        },
                        new[]
                        {
                            "the final embrace",
                            "the living heart",
                            "the human bond",
                            "every familiar voice",
                        }
                    ),
                },
                new[]
                {
                    Group(
                        "Bodies and persons",
                        new[]
                        {
                            "the stranger",
                            "the absent one",
                            "the unanswering face",
                            "the distant figure",
                            "the hardened heart",
                            "the turned back",
                            "the forgotten kin",
                        }
                    ),
                    Group(
                        "Objects and locations",
                        new[]
                        {
                            "empty chair",
                            "closed door",
                            "abandoned hearth",
                            "vacant room",
                            "frozen window",
                            "unshared table",
                            "silent house",
                            "distant shore",
                        }
                    ),
                    Group(
                        "Conditions and settings",
                        new[]
                        {
                            "without farewell",
                            "after the final word",
                            "beyond reach",
                            "across the empty room",
                            "beneath an unanswered sky",
                            "outside the open door",
                            "after the fire has died",
                            "where no voice returns",
                        }
                    ),
                    Group(
                        "Consequences",
                        new[]
                        {
                            "left unanswered",
                            "made remote",
                            "severed from kin",
                            "frozen into silence",
                            "abandoned without farewell",
                            "stripped of affection",
                            "closed against all voices",
                            "erased from memory",
                        }
                    ),
                }
            );
        }

        private static TrackNamingTagLexicon CreateWarm()
        {
            return new TrackNamingTagLexicon(
                TagAxis.Emotional,
                TagPole.Positive,
                new[]
                {
                    Degree(
                        TagDegree.Neutral,
                        "Familial",
                        new[]
                        {
                            "familial",
                            "familiar",
                            "intimate",
                            "gentle",
                        },
                        new[]
                        {
                            "kinship",
                            "home",
                            "company",
                            "nearness",
                        },
                        new[]
                        {
                            "gathering",
                            "recognizing",
                            "tending",
                            "holding close",
                        },
                        new[]
                        {
                            "the shared table",
                            "the familiar hand",
                            "the household",
                            "the returning kin",
                        }
                    ),
                    Degree(
                        TagDegree.Weak,
                        "Welcoming",
                        new[]
                        {
                            "welcoming",
                            "open",
                            "hospitable",
                            "tender",
                        },
                        new[]
                        {
                            "welcome",
                            "shelter",
                            "fellowship",
                            "invitation",
                        },
                        new[]
                        {
                            "receiving",
                            "inviting",
                            "comforting",
                            "opening to",
                        },
                        new[]
                        {
                            "the stranger",
                            "the exile",
                            "the returning voice",
                            "the one outside",
                        }
                    ),
                    Degree(
                        TagDegree.Dominant,
                        "Embracing",
                        new[]
                        {
                            "embracing",
                            "affectionate",
                            "entwined",
                            "ardent",
                        },
                        new[]
                        {
                            "embrace",
                            "affection",
                            "union",
                            "closeness",
                        },
                        new[]
                        {
                            "embracing",
                            "reconciling",
                            "drawing near",
                            "sheltering",
                        },
                        new[]
                        {
                            "the abandoned one",
                            "the estranged heart",
                            "the cold hand",
                            "the empty room",
                        }
                    ),
                    Degree(
                        TagDegree.Transgressive,
                        "Fervent",
                        new[]
                        {
                            "fervent",
                            "passionate",
                            "consuming",
                            "ecstatic",
                        },
                        new[]
                        {
                            "fervour",
                            "communion",
                            "ardour",
                            "devotion",
                        },
                        new[]
                        {
                            "igniting",
                            "enveloping",
                            "binding",
                            "consuming in affection",
                        },
                        new[]
                        {
                            "the frozen heart",
                            "the final distance",
                            "all separation",
                            "the last lonely voice",
                        }
                    ),
                },
                new[]
                {
                    Group(
                        "Bodies and persons",
                        new[]
                        {
                            "the beloved",
                            "the returning one",
                            "the welcomed stranger",
                            "the familiar face",
                            "the open hand",
                            "the gathered kin",
                            "the embraced exile",
                        }
                    ),
                    Group(
                        "Objects and locations",
                        new[]
                        {
                            "hearth",
                            "open door",
                            "shared table",
                            "crowded room",
                            "family home",
                            "offered shelter",
                            "joined hands",
                            "common fire",
                        }
                    ),
                    Group(
                        "Conditions and settings",
                        new[]
                        {
                            "at homecoming",
                            "around the fire",
                            "within the embrace",
                            "before the open door",
                            "among kin",
                            "beneath a familiar roof",
                            "at the shared table",
                            "where every voice is answered",
                        }
                    ),
                    Group(
                        "Consequences",
                        new[]
                        {
                            "drawn near",
                            "taken in",
                            "held in common",
                            "welcomed home",
                            "bound by affection",
                            "restored to kin",
                            "sheltered from distance",
                            "consumed by fervour",
                        }
                    ),
                }
            );
        }

        private static TrackNamingTagLexicon CreateRaw()
        {
            return new TrackNamingTagLexicon(
                TagAxis.Expressive,
                TagPole.Negative,
                new[]
                {
                    Degree(
                        TagDegree.Neutral,
                        "Rough",
                        new[]
                        {
                            "rough",
                            "coarse",
                            "uneven",
                            "scraped",
                        },
                        new[]
                        {
                            "grit",
                            "abrasion",
                            "splinter",
                            "noise",
                        },
                        new[]
                        {
                            "scraping",
                            "scuffing",
                            "dragging",
                            "leaving unfinished",
                        },
                        new[]
                        {
                            "the rough edge",
                            "the cracked surface",
                            "the splintered form",
                            "an uneven voice",
                        }
                    ),
                    Degree(
                        TagDegree.Weak,
                        "Unpolished",
                        new[]
                        {
                            "unpolished",
                            "jagged",
                            "crude",
                            "distorted",
                        },
                        new[]
                        {
                            "distortion",
                            "feedback",
                            "fracture",
                            "static",
                        },
                        new[]
                        {
                            "tearing",
                            "distorting",
                            "cracking",
                            "wrenching apart",
                        },
                        new[]
                        {
                            "the broken rhythm",
                            "the exposed wire",
                            "the unfinished shape",
                            "the fractured signal",
                        }
                    ),
                    Degree(
                        TagDegree.Dominant,
                        "Primitive",
                        new[]
                        {
                            "primitive",
                            "feral",
                            "instinctive",
                            "hewn",
                        },
                        new[]
                        {
                            "instinct",
                            "hammering",
                            "rubble",
                            "animal noise",
                        },
                        new[]
                        {
                            "bludgeoning",
                            "hacking",
                            "reducing",
                            "beating into shape",
                        },
                        new[]
                        {
                            "the carved bone",
                            "the crude weapon",
                            "the beaten frame",
                            "the primal pulse",
                        }
                    ),
                    Degree(
                        TagDegree.Transgressive,
                        "Brutal",
                        new[]
                        {
                            "brutal",
                            "savage",
                            "mangled",
                            "catastrophic",
                        },
                        new[]
                        {
                            "brutality",
                            "rupture",
                            "carnage",
                            "total noise",
                        },
                        new[]
                        {
                            "pulverizing",
                            "mutilating",
                            "shattering",
                            "destroying beyond repair",
                        },
                        new[]
                        {
                            "the ruined form",
                            "the bleeding signal",
                            "the crushed instrument",
                            "all remaining structure",
                        }
                    ),
                },
                new[]
                {
                    Group(
                        "Materials and instruments",
                        new[]
                        {
                            "splintered wood",
                            "rusted wire",
                            "broken strings",
                            "cracked stone",
                            "bare hands",
                            "blunt iron",
                            "torn skin",
                            "exposed circuitry",
                        }
                    ),
                    Group(
                        "Sounds and structures",
                        new[]
                        {
                            "feedback",
                            "static",
                            "animal noise",
                            "fractured rhythm",
                            "collapsing pulse",
                            "torn melody",
                            "broken signal",
                            "unmetered hammering",
                        }
                    ),
                    Group(
                        "Conditions and settings",
                        new[]
                        {
                            "without rehearsal",
                            "before the first take ends",
                            "beneath the feedback",
                            "inside the collapsing rhythm",
                            "through a cracked amplifier",
                            "with bare hands",
                            "beyond all tuning",
                            "where structure gives way",
                        }
                    ),
                    Group(
                        "Consequences",
                        new[]
                        {
                            "left unfinished",
                            "torn out of shape",
                            "reduced to noise",
                            "stripped of polish",
                            "beaten into instinct",
                            "shattered beyond measure",
                            "exposed without restraint",
                            "dragged through distortion",
                        }
                    ),
                }
            );
        }

        private static TrackNamingTagLexicon CreateHoned()
        {
            return new TrackNamingTagLexicon(
                TagAxis.Expressive,
                TagPole.Positive,
                new[]
                {
                    Degree(
                        TagDegree.Neutral,
                        "Polished",
                        new[]
                        {
                            "polished",
                            "balanced",
                            "clean",
                            "shaped",
                        },
                        new[]
                        {
                            "polish",
                            "balance",
                            "form",
                            "clarity",
                        },
                        new[]
                        {
                            "smoothing",
                            "shaping",
                            "balancing",
                            "clearing",
                        },
                        new[]
                        {
                            "the clean edge",
                            "the measured phrase",
                            "the shaped surface",
                            "the balanced form",
                        }
                    ),
                    Degree(
                        TagDegree.Weak,
                        "Refined",
                        new[]
                        {
                            "refined",
                            "disciplined",
                            "controlled",
                            "tempered",
                        },
                        new[]
                        {
                            "refinement",
                            "discipline",
                            "craft",
                            "control",
                        },
                        new[]
                        {
                            "refining",
                            "tempering",
                            "arranging",
                            "correcting",
                        },
                        new[]
                        {
                            "the sharpened line",
                            "the disciplined hand",
                            "the ordered rhythm",
                            "the tempered blade",
                        }
                    ),
                    Degree(
                        TagDegree.Dominant,
                        "Precise",
                        new[]
                        {
                            "precise",
                            "exact",
                            "surgical",
                            "calculated",
                        },
                        new[]
                        {
                            "precision",
                            "geometry",
                            "incision",
                            "measure",
                        },
                        new[]
                        {
                            "cutting",
                            "aligning",
                            "calibrating",
                            "dissecting",
                        },
                        new[]
                        {
                            "the exact wound",
                            "the measured cut",
                            "the calibrated pulse",
                            "the geometric form",
                        }
                    ),
                    Degree(
                        TagDegree.Transgressive,
                        "Masterful",
                        new[]
                        {
                            "masterful",
                            "flawless",
                            "perfected",
                            "absolute",
                        },
                        new[]
                        {
                            "mastery",
                            "perfection",
                            "command",
                            "immaculate form",
                        },
                        new[]
                        {
                            "perfecting",
                            "commanding",
                            "carving without error",
                            "reducing to exactness",
                        },
                        new[]
                        {
                            "the final form",
                            "the flawless edge",
                            "the completed design",
                            "absolute control",
                        }
                    ),
                },
                new[]
                {
                    Group(
                        "Materials and instruments",
                        new[]
                        {
                            "sharpened blade",
                            "tempered steel",
                            "measured hand",
                            "polished surface",
                            "calibrated instrument",
                            "drafting compass",
                            "surgeon’s knife",
                            "flawless mechanism",
                        }
                    ),
                    Group(
                        "Sounds and structures",
                        new[]
                        {
                            "exact rhythm",
                            "controlled pulse",
                            "measured phrase",
                            "aligned voices",
                            "geometric cadence",
                            "disciplined silence",
                            "calculated harmony",
                            "perfected sequence",
                        }
                    ),
                    Group(
                        "Conditions and settings",
                        new[]
                        {
                            "under measured hands",
                            "within the final arrangement",
                            "along the sharpened edge",
                            "at the point of incision",
                            "beneath flawless control",
                            "without a wasted movement",
                            "inside the perfected form",
                            "where every strike is counted",
                        }
                    ),
                    Group(
                        "Consequences",
                        new[]
                        {
                            "brought into order",
                            "cut to measure",
                            "shaped without error",
                            "stripped of excess",
                            "disciplined into form",
                            "sharpened to perfection",
                            "completed beyond revision",
                            "reduced to exact proportion",
                        }
                    ),
                }
            );
        }

        private static TrackNamingTagLexicon CreateFast()
        {
            return new TrackNamingTagLexicon(
                TagAxis.Temporal,
                TagPole.Negative,
                new[]
                {
                    Degree(
                        TagDegree.Neutral,
                        "Urgent",
                        new[]
                        {
                            "urgent",
                            "hurried",
                            "immediate",
                            "restless",
                        },
                        new[]
                        {
                            "haste",
                            "alarm",
                            "impulse",
                            "departure",
                        },
                        new[]
                        {
                            "hurrying",
                            "calling forth",
                            "awakening",
                            "pressing onward",
                        },
                        new[]
                        {
                            "the sudden warning",
                            "the open road",
                            "the restless step",
                            "the approaching hour",
                        }
                    ),
                    Degree(
                        TagDegree.Weak,
                        "Driving",
                        new[]
                        {
                            "driving",
                            "rushing",
                            "relentless",
                            "propulsive",
                        },
                        new[]
                        {
                            "momentum",
                            "pursuit",
                            "charge",
                            "acceleration",
                        },
                        new[]
                        {
                            "pursuing",
                            "overtaking",
                            "driving through",
                            "forcing onward",
                        },
                        new[]
                        {
                            "the fleeing figure",
                            "the turning wheel",
                            "the closing distance",
                            "the racing pulse",
                        }
                    ),
                    Degree(
                        TagDegree.Dominant,
                        "Frenetic",
                        new[]
                        {
                            "frenetic",
                            "fevered",
                            "convulsive",
                            "uncontrolled",
                        },
                        new[]
                        {
                            "frenzy",
                            "stampede",
                            "panic",
                            "eruption",
                        },
                        new[]
                        {
                            "scattering",
                            "surging through",
                            "tearing past",
                            "outrunning",
                        },
                        new[]
                        {
                            "the broken formation",
                            "the collapsing procession",
                            "the panicked crowd",
                            "the failing rhythm",
                        }
                    ),
                    Degree(
                        TagDegree.Transgressive,
                        "Blistering",
                        new[]
                        {
                            "blistering",
                            "instantaneous",
                            "explosive",
                            "terminal",
                        },
                        new[]
                        {
                            "detonation",
                            "firestorm",
                            "velocity",
                            "total acceleration",
                        },
                        new[]
                        {
                            "incinerating",
                            "crossing in an instant",
                            "obliterating in passage",
                            "burning beyond control",
                        },
                        new[]
                        {
                            "the final barrier",
                            "all remaining distance",
                            "the last measured breath",
                            "the world left behind",
                        }
                    ),
                },
                new[]
                {
                    Group(
                        "Bodies and persons",
                        new[]
                        {
                            "the runner",
                            "the pursued",
                            "the breathless messenger",
                            "the fleeing host",
                            "the racing heart",
                            "the impatient hand",
                            "the one who cannot stop",
                        }
                    ),
                    Group(
                        "Objects and locations",
                        new[]
                        {
                            "turning wheel",
                            "open road",
                            "burning track",
                            "collapsing bridge",
                            "closing gate",
                            "fractured clock",
                            "racing current",
                            "path of sparks",
                        }
                    ),
                    Group(
                        "Conditions and settings",
                        new[]
                        {
                            "before the warning ends",
                            "at the first alarm",
                            "without preparation",
                            "between two heartbeats",
                            "ahead of the pursuing fire",
                            "before the gate can close",
                            "while the clock is breaking",
                            "where no step can settle",
                        }
                    ),
                    Group(
                        "Consequences",
                        new[]
                        {
                            "driven beyond control",
                            "overtaken without warning",
                            "scattered in passage",
                            "left gasping behind",
                            "carried past return",
                            "burned through in an instant",
                            "outrun by consequence",
                            "reduced to momentum",
                        }
                    ),
                }
            );
        }

        private static TrackNamingTagLexicon CreateSlow()
        {
            return new TrackNamingTagLexicon(
                TagAxis.Temporal,
                TagPole.Positive,
                new[]
                {
                    Degree(
                        TagDegree.Neutral,
                        "Measured",
                        new[]
                        {
                            "measured",
                            "patient",
                            "deliberate",
                            "gradual",
                        },
                        new[]
                        {
                            "patience",
                            "interval",
                            "procession",
                            "pause",
                        },
                        new[]
                        {
                            "waiting",
                            "pacing",
                            "counting",
                            "approaching carefully",
                        },
                        new[]
                        {
                            "the measured step",
                            "the patient hand",
                            "the long road",
                            "the waiting hour",
                        }
                    ),
                    Degree(
                        TagDegree.Weak,
                        "Heavy",
                        new[]
                        {
                            "heavy",
                            "burdened",
                            "ponderous",
                            "weighty",
                        },
                        new[]
                        {
                            "burden",
                            "weight",
                            "march",
                            "pressure",
                        },
                        new[]
                        {
                            "burdening",
                            "weighing down",
                            "slowing",
                            "pressing upon",
                        },
                        new[]
                        {
                            "the bent back",
                            "the iron step",
                            "the burdened road",
                            "the sinking body",
                        }
                    ),
                    Degree(
                        TagDegree.Dominant,
                        "Dragging",
                        new[]
                        {
                            "dragging",
                            "exhausted",
                            "prolonged",
                            "grinding",
                        },
                        new[]
                        {
                            "delay",
                            "exhaustion",
                            "attrition",
                            "endless passage",
                        },
                        new[]
                        {
                            "dragging",
                            "restraining",
                            "grinding down",
                            "drawing out",
                        },
                        new[]
                        {
                            "the wounded procession",
                            "the final mile",
                            "the exhausted breath",
                            "the unending night",
                        }
                    ),
                    Degree(
                        TagDegree.Transgressive,
                        "Monolithic",
                        new[]
                        {
                            "monolithic",
                            "immovable",
                            "eternal",
                            "geological",
                        },
                        new[]
                        {
                            "monolith",
                            "inevitability",
                            "stasis",
                            "endless weight",
                        },
                        new[]
                        {
                            "arresting",
                            "entombing",
                            "fixing in place",
                            "crushing beneath duration",
                        },
                        new[]
                        {
                            "the halted world",
                            "the buried impulse",
                            "all attempted motion",
                            "the age without end",
                        }
                    ),
                },
                new[]
                {
                    Group(
                        "Bodies and persons",
                        new[]
                        {
                            "the bearer",
                            "the waiting figure",
                            "the exhausted pilgrim",
                            "the bent procession",
                            "the ancient witness",
                            "the one who remains",
                            "the body beneath the burden",
                        }
                    ),
                    Group(
                        "Objects and locations",
                        new[]
                        {
                            "stone road",
                            "iron gate",
                            "burial mound",
                            "endless stair",
                            "stalled clock",
                            "black monolith",
                            "frozen procession",
                            "mountain pass",
                        }
                    ),
                    Group(
                        "Conditions and settings",
                        new[]
                        {
                            "one step at a time",
                            "beneath the accumulated years",
                            "through the unending night",
                            "after all urgency has failed",
                            "under the weight of stone",
                            "across the final mile",
                            "where the clock has stopped",
                            "until movement becomes memory",
                        }
                    ),
                    Group(
                        "Consequences",
                        new[]
                        {
                            "brought to a halt",
                            "burdened beyond movement",
                            "drawn through the years",
                            "fixed beneath the weight",
                            "ground down by duration",
                            "left waiting without end",
                            "reduced to stillness",
                            "buried beneath inevitability",
                        }
                    ),
                }
            );
        }

        private static TrackNamingTagLexicon CreateMalevolent()
        {
            return new TrackNamingTagLexicon(
                TagAxis.Physical,
                TagPole.Negative,
                new[]
                {
                    Degree(
                        TagDegree.Neutral,
                        "Irritable",
                        new[]
                        {
                            "irritable",
                            "hostile",
                            "sour",
                            "agitated",
                        },
                        new[]
                        {
                            "irritation",
                            "quarrel",
                            "anger",
                            "grievance",
                        },
                        new[]
                        {
                            "snapping at",
                            "provoking",
                            "pushing away",
                            "turning against",
                        },
                        new[]
                        {
                            "the hostile glance",
                            "the raised voice",
                            "the petty grievance",
                            "the clenched hand",
                        }
                    ),
                    Degree(
                        TagDegree.Weak,
                        "Spiteful",
                        new[]
                        {
                            "spiteful",
                            "hateful",
                            "resentful",
                            "venomous",
                        },
                        new[]
                        {
                            "spite",
                            "hatred",
                            "venom",
                            "insult",
                        },
                        new[]
                        {
                            "insulting",
                            "humiliating",
                            "wounding",
                            "denying",
                        },
                        new[]
                        {
                            "the rival",
                            "the trusting face",
                            "the offered peace",
                            "the wounded pride",
                        }
                    ),
                    Degree(
                        TagDegree.Dominant,
                        "Vindictive",
                        new[]
                        {
                            "vindictive",
                            "vengeful",
                            "punishing",
                            "merciless",
                        },
                        new[]
                        {
                            "vengeance",
                            "retaliation",
                            "punishment",
                            "blood-debt",
                        },
                        new[]
                        {
                            "avenging",
                            "hunting down",
                            "punishing",
                            "repaying in blood",
                        },
                        new[]
                        {
                            "the guilty one",
                            "the former friend",
                            "the remembered injury",
                            "the pleading enemy",
                        }
                    ),
                    Degree(
                        TagDegree.Transgressive,
                        "Malicious",
                        new[]
                        {
                            "malicious",
                            "cruel",
                            "sadistic",
                            "ruinous",
                        },
                        new[]
                        {
                            "malice",
                            "cruelty",
                            "torment",
                            "deliberate ruin",
                        },
                        new[]
                        {
                            "torturing",
                            "destroying",
                            "corrupting",
                            "harming for pleasure",
                        },
                        new[]
                        {
                            "the helpless",
                            "the innocent",
                            "the final survivor",
                            "everything still unharmed",
                        }
                    ),
                },
                new[]
                {
                    Group(
                        "Bodies and persons",
                        new[]
                        {
                            "the rival",
                            "the former friend",
                            "the accused",
                            "the helpless one",
                            "the pleading enemy",
                            "the trusting face",
                            "the wounded kin",
                            "the final survivor",
                        }
                    ),
                    Group(
                        "Objects and instruments",
                        new[]
                        {
                            "clenched fist",
                            "rusted knife",
                            "poisoned cup",
                            "bloodied letter",
                            "broken promise",
                            "sharpened grievance",
                            "chain of revenge",
                            "ledger of debts",
                        }
                    ),
                    Group(
                        "Conditions and settings",
                        new[]
                        {
                            "after the insult",
                            "before forgiveness",
                            "beneath the accusing eye",
                            "at the settling of debts",
                            "after trust has failed",
                            "within the old grievance",
                            "before the wound can close",
                            "where mercy is refused",
                        }
                    ),
                    Group(
                        "Consequences",
                        new[]
                        {
                            "left to suffer",
                            "marked for revenge",
                            "punished beyond measure",
                            "repaid in blood",
                            "stripped of mercy",
                            "broken out of spite",
                            "preserved only for torment",
                            "ruined without necessity",
                        }
                    ),
                }
            );
        }

        private static TrackNamingTagLexicon CreateBenevolent()
        {
            return new TrackNamingTagLexicon(
                TagAxis.Physical,
                TagPole.Positive,
                new[]
                {
                    Degree(
                        TagDegree.Neutral,
                        "Kind",
                        new[]
                        {
                            "kind",
                            "gentle",
                            "considerate",
                            "merciful",
                        },
                        new[]
                        {
                            "kindness",
                            "mercy",
                            "aid",
                            "goodwill",
                        },
                        new[]
                        {
                            "helping",
                            "tending",
                            "forgiving",
                            "easing",
                        },
                        new[]
                        {
                            "the injured stranger",
                            "the tired hand",
                            "the minor wound",
                            "the one in need",
                        }
                    ),
                    Degree(
                        TagDegree.Weak,
                        "Generous",
                        new[]
                        {
                            "generous",
                            "giving",
                            "charitable",
                            "selfless",
                        },
                        new[]
                        {
                            "generosity",
                            "offering",
                            "relief",
                            "gift",
                        },
                        new[]
                        {
                            "giving to",
                            "feeding",
                            "sharing with",
                            "relieving",
                        },
                        new[]
                        {
                            "the hungry",
                            "the dispossessed",
                            "the empty hand",
                            "the abandoned household",
                        }
                    ),
                    Degree(
                        TagDegree.Dominant,
                        "Protective",
                        new[]
                        {
                            "protective",
                            "vigilant",
                            "sheltering",
                            "steadfast",
                        },
                        new[]
                        {
                            "protection",
                            "shelter",
                            "guardianship",
                            "defence",
                        },
                        new[]
                        {
                            "defending",
                            "shielding",
                            "rescuing",
                            "standing before",
                        },
                        new[]
                        {
                            "the threatened child",
                            "the wounded kin",
                            "the hunted one",
                            "the vulnerable",
                        }
                    ),
                    Degree(
                        TagDegree.Transgressive,
                        "Compassionate",
                        new[]
                        {
                            "compassionate",
                            "sacrificial",
                            "all-forgiving",
                            "boundlessly merciful",
                        },
                        new[]
                        {
                            "compassion",
                            "salvation",
                            "sacrifice",
                            "deliverance",
                        },
                        new[]
                        {
                            "sacrificing for",
                            "redeeming",
                            "preserving at any cost",
                            "suffering in place of",
                        },
                        new[]
                        {
                            "the condemned",
                            "the enemy",
                            "every wounded life",
                            "those beyond forgiveness",
                        }
                    ),
                },
                new[]
                {
                    Group(
                        "Bodies and persons",
                        new[]
                        {
                            "the wounded one",
                            "the stranger",
                            "the hungry",
                            "the abandoned child",
                            "the hunted",
                            "the forgiven enemy",
                            "the returning exile",
                            "the one left behind",
                        }
                    ),
                    Group(
                        "Objects and locations",
                        new[]
                        {
                            "open hand",
                            "shared bread",
                            "offered coat",
                            "guarded doorway",
                            "place of refuge",
                            "healing room",
                            "sheltering wall",
                            "common store",
                        }
                    ),
                    Group(
                        "Conditions and settings",
                        new[]
                        {
                            "without asking payment",
                            "before the blow can fall",
                            "beneath the sheltering hand",
                            "at the hour of need",
                            "after all others have left",
                            "before judgment",
                            "among the wounded",
                            "where no one else will stand",
                        }
                    ),
                    Group(
                        "Consequences",
                        new[]
                        {
                            "granted mercy",
                            "spared from harm",
                            "carried to safety",
                            "defended without reward",
                            "restored through kindness",
                            "forgiven beyond reason",
                            "preserved at great cost",
                            "redeemed from ruin",
                        }
                    ),
                }
            );
        }

        private static TrackNamingTagLexicon CreateMorbid()
        {
            return new TrackNamingTagLexicon(
                TagAxis.Existential,
                TagPole.Negative,
                new[]
                {
                    Degree(
                        TagDegree.Neutral,
                        "Gloomy",
                        new[]
                        {
                            "gloomy",
                            "waning",
                            "mournful",
                            "grey",
                        },
                        new[]
                        {
                            "gloom",
                            "sorrow",
                            "dusk",
                            "decline",
                        },
                        new[]
                        {
                            "mourning",
                            "dimming",
                            "withering",
                            "lingering over",
                        },
                        new[]
                        {
                            "the fading light",
                            "the empty bed",
                            "the dying day",
                            "the quiet grave",
                        }
                    ),
                    Degree(
                        TagDegree.Weak,
                        "Macabre",
                        new[]
                        {
                            "macabre",
                            "deathly",
                            "cadaverous",
                            "grisly",
                        },
                        new[]
                        {
                            "corpse",
                            "remains",
                            "pallor",
                            "decay",
                        },
                        new[]
                        {
                            "exposing",
                            "exhuming",
                            "rotting",
                            "arranging for burial",
                        },
                        new[]
                        {
                            "the opened grave",
                            "the pale body",
                            "the funeral cloth",
                            "the waiting coffin",
                        }
                    ),
                    Degree(
                        TagDegree.Dominant,
                        "Funereal",
                        new[]
                        {
                            "funereal",
                            "mortuary",
                            "entombed",
                            "death-bound",
                        },
                        new[]
                        {
                            "funeral",
                            "burial",
                            "tomb",
                            "procession",
                        },
                        new[]
                        {
                            "burying",
                            "embalming",
                            "entombing",
                            "carrying to the grave",
                        },
                        new[]
                        {
                            "the final breath",
                            "the funeral host",
                            "the buried name",
                            "the sealed crypt",
                        }
                    ),
                    Degree(
                        TagDegree.Transgressive,
                        "Sepulchral",
                        new[]
                        {
                            "sepulchral",
                            "necrotic",
                            "ossified",
                            "deathless",
                        },
                        new[]
                        {
                            "sepulchre",
                            "charnel house",
                            "extinction",
                            "eternal burial",
                        },
                        new[]
                        {
                            "reducing to remains",
                            "sealing beneath stone",
                            "extinguishing",
                            "returning everything to dust",
                        },
                        new[]
                        {
                            "the last living body",
                            "every beating heart",
                            "the unburied world",
                            "all remaining life",
                        }
                    ),
                },
                new[]
                {
                    Group(
                        "Bodies and persons",
                        new[]
                        {
                            "the corpse",
                            "the mourner",
                            "the pale child",
                            "the dying one",
                            "the buried king",
                            "the final witness",
                            "the unnamed dead",
                            "the body without breath",
                        }
                    ),
                    Group(
                        "Objects and locations",
                        new[]
                        {
                            "coffin",
                            "grave",
                            "tomb",
                            "burial mound",
                            "charnel house",
                            "funeral cloth",
                            "ossuary",
                            "sealed crypt",
                        }
                    ),
                    Group(
                        "Conditions and settings",
                        new[]
                        {
                            "beneath the funeral bell",
                            "after the final breath",
                            "among the unburied",
                            "inside the opened grave",
                            "beneath the weight of earth",
                            "at the edge of extinction",
                            "after the mourners depart",
                            "where the dead remain awake",
                        }
                    ),
                    Group(
                        "Consequences",
                        new[]
                        {
                            "carried to the grave",
                            "left among the dead",
                            "reduced to remains",
                            "sealed beneath stone",
                            "stripped of breath",
                            "returned to dust",
                            "preserved in decay",
                            "forgotten beneath the earth",
                        }
                    ),
                }
            );
        }

        private static TrackNamingTagLexicon CreateVital()
        {
            return new TrackNamingTagLexicon(
                TagAxis.Existential,
                TagPole.Positive,
                new[]
                {
                    Degree(
                        TagDegree.Neutral,
                        "Lively",
                        new[]
                        {
                            "lively",
                            "breathing",
                            "fresh",
                            "awake",
                        },
                        new[]
                        {
                            "breath",
                            "pulse",
                            "awakening",
                            "motion",
                        },
                        new[]
                        {
                            "breathing into",
                            "stirring",
                            "awakening",
                            "moving through",
                        },
                        new[]
                        {
                            "the open air",
                            "the waking body",
                            "the first pulse",
                            "the living hand",
                        }
                    ),
                    Degree(
                        TagDegree.Weak,
                        "Energetic",
                        new[]
                        {
                            "energetic",
                            "vigorous",
                            "restless",
                            "surging",
                        },
                        new[]
                        {
                            "energy",
                            "heartbeat",
                            "current",
                            "movement",
                        },
                        new[]
                        {
                            "reviving",
                            "quickening",
                            "driving",
                            "raising",
                        },
                        new[]
                        {
                            "the exhausted body",
                            "the dormant seed",
                            "the weakened heart",
                            "the fallen one",
                        }
                    ),
                    Degree(
                        TagDegree.Dominant,
                        "Thriving",
                        new[]
                        {
                            "thriving",
                            "fertile",
                            "flourishing",
                            "resilient",
                        },
                        new[]
                        {
                            "growth",
                            "renewal",
                            "abundance",
                            "regeneration",
                        },
                        new[]
                        {
                            "restoring",
                            "blooming through",
                            "regenerating",
                            "multiplying within",
                        },
                        new[]
                        {
                            "the ruined field",
                            "the wounded flesh",
                            "the barren ground",
                            "the broken lineage",
                        }
                    ),
                    Degree(
                        TagDegree.Transgressive,
                        "Exuberant",
                        new[]
                        {
                            "exuberant",
                            "overflowing",
                            "rampant",
                            "irrepressible",
                        },
                        new[]
                        {
                            "vitality",
                            "profusion",
                            "eruption",
                            "living abundance",
                        },
                        new[]
                        {
                            "overwhelming",
                            "bursting from",
                            "resurrecting",
                            "consuming through growth",
                        },
                        new[]
                        {
                            "the sealed tomb",
                            "the final extinction",
                            "every barren place",
                            "the dominion of death",
                        }
                    ),
                },
                new[]
                {
                    Group(
                        "Bodies and persons",
                        new[]
                        {
                            "the newborn",
                            "the survivor",
                            "the waking body",
                            "the breathing host",
                            "the restored one",
                            "the fertile mother",
                            "the beating heart",
                            "the one returned to life",
                        }
                    ),
                    Group(
                        "Objects and locations",
                        new[]
                        {
                            "seed",
                            "root",
                            "bloodstream",
                            "open field",
                            "green branch",
                            "spring water",
                            "living soil",
                            "crowded nest",
                        }
                    ),
                    Group(
                        "Conditions and settings",
                        new[]
                        {
                            "at first light",
                            "beneath the returning sun",
                            "within the bloodstream",
                            "after the wound has closed",
                            "among the growing roots",
                            "where the earth breaks open",
                            "at the first returning breath",
                            "beyond the season of death",
                        }
                    ),
                    Group(
                        "Consequences",
                        new[]
                        {
                            "restored to breath",
                            "raised from weakness",
                            "returned to motion",
                            "overgrown with life",
                            "made fertile again",
                            "renewed beyond injury",
                            "carried into abundance",
                            "awakened beyond death",
                        }
                    ),
                }
            );
        }

        private static TrackNamingTagLexicon CreateVoid()
        {
            return new TrackNamingTagLexicon(
                TagAxis.Interpretive,
                TagPole.Negative,
                new[]
                {
                    Degree(
                        TagDegree.Neutral,
                        "Hollow",
                        new[]
                        {
                            "hollow",
                            "vacant",
                            "empty",
                            "aimless",
                        },
                        new[]
                        {
                            "emptiness",
                            "absence",
                            "vacancy",
                            "echo",
                        },
                        new[]
                        {
                            "emptying",
                            "abandoning",
                            "overlooking",
                            "stripping significance from",
                        },
                        new[]
                        {
                            "the unanswered question",
                            "the vacant sign",
                            "the forgotten name",
                            "the aimless road",
                        }
                    ),
                    Degree(
                        TagDegree.Weak,
                        "Nihilistic",
                        new[]
                        {
                            "nihilistic",
                            "purposeless",
                            "futile",
                            "meaningless",
                        },
                        new[]
                        {
                            "futility",
                            "negation",
                            "nothingness",
                            "denial",
                        },
                        new[]
                        {
                            "denying",
                            "negating",
                            "dismissing",
                            "erasing meaning from",
                        },
                        new[]
                        {
                            "the promise",
                            "the cause",
                            "the oath",
                            "the chosen direction",
                        }
                    ),
                    Degree(
                        TagDegree.Dominant,
                        "Bleak",
                        new[]
                        {
                            "bleak",
                            "hopeless",
                            "desolate",
                            "terminal",
                        },
                        new[]
                        {
                            "despair",
                            "desolation",
                            "ruin",
                            "final doubt",
                        },
                        new[]
                        {
                            "hollowing out",
                            "extinguishing hope in",
                            "reducing to futility",
                            "abandoning beyond return",
                        },
                        new[]
                        {
                            "the guiding star",
                            "the final hope",
                            "the last reason",
                            "the imagined future",
                        }
                    ),
                    Degree(
                        TagDegree.Transgressive,
                        "Abyssal",
                        new[]
                        {
                            "abyssal",
                            "bottomless",
                            "annihilating",
                            "absolute",
                        },
                        new[]
                        {
                            "abyss",
                            "oblivion",
                            "nonbeing",
                            "total negation",
                        },
                        new[]
                        {
                            "devouring purpose",
                            "unmaking meaning",
                            "erasing every name",
                            "returning all to nothing",
                        },
                        new[]
                        {
                            "the final answer",
                            "every remembered name",
                            "all purpose",
                            "the world of signs",
                        }
                    ),
                },
                new[]
                {
                    Group(
                        "Bodies and persons",
                        new[]
                        {
                            "the nameless one",
                            "the unbeliever",
                            "the abandoned seeker",
                            "the final doubter",
                            "the one without purpose",
                            "the forgotten witness",
                            "the unanswered voice",
                            "the person who no longer asks",
                        }
                    ),
                    Group(
                        "Objects and locations",
                        new[]
                        {
                            "empty page",
                            "unmarked grave",
                            "vacant throne",
                            "broken compass",
                            "abandoned road",
                            "lightless chamber",
                            "erased inscription",
                            "bottomless pit",
                        }
                    ),
                    Group(
                        "Conditions and settings",
                        new[]
                        {
                            "without explanation",
                            "after every answer has failed",
                            "beyond the final sign",
                            "beneath an empty sky",
                            "where no path remains",
                            "after the name is forgotten",
                            "at the end of all questions",
                            "where nothing points home",
                        }
                    ),
                    Group(
                        "Consequences",
                        new[]
                        {
                            "left without meaning",
                            "reduced to nothing",
                            "emptied of purpose",
                            "forgotten without explanation",
                            "stripped of significance",
                            "abandoned beyond hope",
                            "erased from interpretation",
                            "swallowed by the abyss",
                        }
                    ),
                }
            );
        }

        private static TrackNamingTagLexicon CreateMeaning()
        {
            return new TrackNamingTagLexicon(
                TagAxis.Interpretive,
                TagPole.Positive,
                new[]
                {
                    Degree(
                        TagDegree.Neutral,
                        "Purposeful",
                        new[]
                        {
                            "purposeful",
                            "directed",
                            "deliberate",
                            "significant",
                        },
                        new[]
                        {
                            "purpose",
                            "direction",
                            "task",
                            "sign",
                        },
                        new[]
                        {
                            "assigning",
                            "naming",
                            "guiding",
                            "giving direction to",
                        },
                        new[]
                        {
                            "the chosen path",
                            "the marked stone",
                            "the answered question",
                            "the deliberate act",
                        }
                    ),
                    Degree(
                        TagDegree.Weak,
                        "Devoted",
                        new[]
                        {
                            "devoted",
                            "committed",
                            "steadfast",
                            "dedicated",
                        },
                        new[]
                        {
                            "devotion",
                            "duty",
                            "vow",
                            "cause",
                        },
                        new[]
                        {
                            "dedicating",
                            "binding to purpose",
                            "serving",
                            "keeping faith with",
                        },
                        new[]
                        {
                            "the oath",
                            "the chosen cause",
                            "the entrusted task",
                            "the promised destination",
                        }
                    ),
                    Degree(
                        TagDegree.Dominant,
                        "Zealous",
                        new[]
                        {
                            "zealous",
                            "fervent",
                            "uncompromising",
                            "mission-bound",
                        },
                        new[]
                        {
                            "mission",
                            "conviction",
                            "mandate",
                            "destiny",
                        },
                        new[]
                        {
                            "proclaiming",
                            "pursuing without rest",
                            "sacrificing to",
                            "forcing into purpose",
                        },
                        new[]
                        {
                            "the final cause",
                            "the revealed doctrine",
                            "the ordained path",
                            "the absolute demand",
                        }
                    ),
                    Degree(
                        TagDegree.Transgressive,
                        "Transcendent",
                        new[]
                        {
                            "transcendent",
                            "revelatory",
                            "ultimate",
                            "world-defining",
                        },
                        new[]
                        {
                            "revelation",
                            "destiny",
                            "cosmic order",
                            "final meaning",
                        },
                        new[]
                        {
                            "transfiguring",
                            "revealing",
                            "naming the world",
                            "making eternal",
                        },
                        new[]
                        {
                            "the last question",
                            "mortal existence",
                            "the silent universe",
                            "all remaining doubt",
                        }
                    ),
                },
                new[]
                {
                    Group(
                        "Bodies and persons",
                        new[]
                        {
                            "the believer",
                            "the seeker",
                            "the devoted one",
                            "the chosen witness",
                            "the bearer of the sign",
                            "the keeper of the vow",
                            "the pilgrim",
                            "the one who knows why",
                        }
                    ),
                    Group(
                        "Objects and locations",
                        new[]
                        {
                            "marked path",
                            "guiding star",
                            "written law",
                            "sworn oath",
                            "revealed symbol",
                            "illuminated page",
                            "chosen destination",
                            "axis of the world",
                        }
                    ),
                    Group(
                        "Conditions and settings",
                        new[]
                        {
                            "beneath the guiding star",
                            "after the revelation",
                            "along the chosen path",
                            "before the final answer",
                            "within the sworn purpose",
                            "where every sign converges",
                            "at the fulfilment of the vow",
                            "beneath the revealed order",
                        }
                    ),
                    Group(
                        "Consequences",
                        new[]
                        {
                            "given direction",
                            "named at last",
                            "bound to purpose",
                            "made significant",
                            "revealed as destiny",
                            "transformed into doctrine",
                            "carried toward fulfilment",
                            "raised beyond doubt",
                        }
                    ),
                }
            );
        }

        private static TrackNamingDegreeLexicon Degree(
            TagDegree degree,
            string degreeLabel,
            string[] surfaceModifiers,
            string[] headNouns,
            string[] dominantActions,
            string[] submissivePhrases)
        {
            return new TrackNamingDegreeLexicon(
                degree,
                degreeLabel,
                surfaceModifiers,
                headNouns,
                dominantActions,
                submissivePhrases
            );
        }

        private static TrackNamingContextFragmentGroup Group(
            string groupLabel,
            string[] fragments)
        {
            return new TrackNamingContextFragmentGroup(
                groupLabel,
                fragments
            );
        }
    }
}