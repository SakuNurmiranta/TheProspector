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
                                                    "crass",
                            "ribald",
                            "disrespectful",
                            "mocking",
},
                        new[]
                        {
                            "mockery",
                            "grime",
                            "insult",
                            "filth",
                                                    "ridicule",
                            "crudity",
                            "jeer",
                            "smear",
},
                        new[]
                        {
                            "mocking",
                            "sneering at",
                            "scoffing at",
                            "smearing",
                                                    "ridiculing",
                            "jeering at",
                            "deriding",
                            "besmirching",
},
                        new[]
                        {
                            "the unclean",
                            "the gutter",
                            "the vulgar mouth",
                            "an idle insult",
                                                    "the solemn word",
                            "the polished custom",
                            "the respectable face",
                            "a pompous rule",
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
                                                    "scornful",
                            "derisive",
                            "impudent",
                            "godless",
},
                        new[]
                        {
                            "spit",
                            "curse",
                            "obscenity",
                            "defiance",
                                                    "heresy",
                            "taunt",
                            "rebuke",
                            "renunciation",
},
                        new[]
                        {
                            "spitting on",
                            "cursing",
                            "heckling",
                            "rejecting",
                                                    "denouncing",
                            "jeering at",
                            "reviling",
                            "renouncing",
},
                        new[]
                        {
                            "the blasphemer",
                            "the profane sign",
                            "the defiant tongue",
                            "forbidden speech",
                                                    "the irreligious oath",
                            "the mocking voice",
                            "the profane gesture",
                            "the insolent creed",
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
                                                    "befouled",
                            "stained",
                            "sullied",
                            "debased",
},
                        new[]
                        {
                            "desecration",
                            "excrement",
                            "contamination",
                            "defilement",
                                                    "pollution",
                            "stain",
                            "foulness",
                            "debasement",
},
                        new[]
                        {
                            "defiling",
                            "fouling",
                            "soiling",
                            "corrupting",
                                                    "staining",
                            "befouling",
                            "sullying",
                            "degrading",
},
                        new[]
                        {
                            "desecrated ground",
                            "polluted flesh",
                            "the fouled image",
                            "the ruined taboo",
                                                    "the stained relic",
                            "the sullied rite",
                            "the debased symbol",
                            "the befouled sanctuary",
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
                                                    "profaned",
                            "damned",
                            "execrated",
                            "taboo-breaking",
},
                        new[]
                        {
                            "sacrilege",
                            "abomination",
                            "violation",
                            "damnation",
                                                    "profanation",
                            "execration",
                            "outrage",
                            "unholiness",
},
                        new[]
                        {
                            "violating",
                            "unmaking",
                            "annihilating",
                            "rendering unholy",
                                                    "profaning",
                            "desecrating beyond repair",
                            "breaking the taboo of",
                            "damning",
},
                        new[]
                        {
                            "the abomination",
                            "the broken taboo",
                            "the unholy throne",
                            "absolute profanity",
                                                    "the profaned covenant",
                            "the shattered prohibition",
                            "the damned sanctuary",
                            "the last sacred boundary",
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
                                                    "respectful",
                            "ritual",
                            "hallowed",
                            "observant",
},
                        new[]
                        {
                            "reverence",
                            "prayer",
                            "rite",
                            "offering",
                                                    "observance",
                            "ceremony",
                            "homage",
                            "benediction",
},
                        new[]
                        {
                            "bowing before",
                            "invoking",
                            "honouring",
                            "blessing",
                                                    "saluting",
                            "commemorating",
                            "observing",
                            "offering to",
},
                        new[]
                        {
                            "the shrine",
                            "the prayer",
                            "the sacred face",
                            "the quiet rite",
                                                    "the votive flame",
                            "the humble shrine",
                            "the spoken blessing",
                            "the simple observance",
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
                                                    "worshipful",
                            "orthodox",
                            "reverential",
                            "believing",
},
                        new[]
                        {
                            "devotion",
                            "sermon",
                            "liturgy",
                            "faith",
                                                    "worship",
                            "creed",
                            "homily",
                            "adoration",
},
                        new[]
                        {
                            "venerating",
                            "preaching",
                            "blessing",
                            "dedicating",
                                                    "worshipping",
                            "reciting for",
                            "kneeling before",
                            "consecrating to",
},
                        new[]
                        {
                            "the altar",
                            "the chapel",
                            "the congregation",
                            "Sunday prayer",
                                                    "the sermon",
                            "the faithful",
                            "the prayer book",
                            "the holy day",
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
                                                    "sacrosanct",
                            "blessed",
                            "hallowed",
                            "pontifical",
},
                        new[]
                        {
                            "consecration",
                            "sacrament",
                            "reliquary",
                            "holy ground",
                                                    "blessing",
                            "ordination",
                            "sanctuary",
                            "holy office",
},
                        new[]
                        {
                            "anointing",
                            "consecrating",
                            "enthroning",
                            "ordaining",
                                                    "blessing",
                            "hallowing",
                            "vesting",
                            "installing",
},
                        new[]
                        {
                            "the consecrated altar",
                            "the holy relic",
                            "the ordained priest",
                            "sanctified ground",
                                                    "the blessed vessel",
                            "the hallowed sanctuary",
                            "the sacred scripture",
                            "the anointed crown",
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
                                                    "numinous",
                            "godlike",
                            "ineffable",
                            "celestial",
},
                        new[]
                        {
                            "sanctity",
                            "revelation",
                            "divine law",
                            "apotheosis",
                                                    "divinity",
                            "ascension",
                            "beatitude",
                            "holy decree",
},
                        new[]
                        {
                            "sanctifying",
                            "canonizing",
                            "transfiguring",
                            "exalting",
                                                    "deifying",
                            "enthroning in heaven",
                            "making inviolable",
                            "revealing as divine",
},
                        new[]
                        {
                            "the final liturgy",
                            "the throne of heaven",
                            "the eternal commandment",
                            "the divine image",
                                                    "the celestial throne",
                            "the immutable scripture",
                            "the sacred cosmos",
                            "the law beyond question",
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
                                                    "cool",
                            "reserved",
                            "disengaged",
                            "faraway",
},
                        new[]
                        {
                            "distance",
                            "absence",
                            "silence",
                            "separation",
                                                    "detachment",
                            "quiet",
                            "remoteness",
                            "reticence",
},
                        new[]
                        {
                            "turning from",
                            "overlooking",
                            "leaving",
                            "withdrawing from",
                                                    "ignoring",
                            "sidestepping",
                            "avoiding",
                            "standing apart from",
},
                        new[]
                        {
                            "the distant gaze",
                            "the empty chair",
                            "the unanswered voice",
                            "the closed room",
                                                    "the unreturned letter",
                            "the vacant doorway",
                            "the far window",
                            "the fading conversation",
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
                                                    "unmoved",
                            "unfeeling",
                            "dispassionate",
                            "standoffish",
},
                        new[]
                        {
                            "indifference",
                            "refusal",
                            "reserve",
                            "disregard",
                                                    "detachment",
                            "apathy",
                            "coolness",
                            "avoidance",
},
                        new[]
                        {
                            "withholding",
                            "refusing",
                            "dismissing",
                            "passing by",
                                                    "ignoring",
                            "shutting out",
                            "turning away from",
                            "keeping distance from",
},
                        new[]
                        {
                            "the offered hand",
                            "the familiar voice",
                            "the open door",
                            "the waiting face",
                                                    "the pleading hand",
                            "the remembered voice",
                            "the offered shelter",
                            "the expectant face",
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
                                                    "icy",
                            "alienated",
                            "rancorous",
                            "cold-hearted",
},
                        new[]
                        {
                            "bitterness",
                            "grievance",
                            "resentment",
                            "frost",
                                                    "estrangement",
                            "rancour",
                            "coldness",
                            "alienation",
},
                        new[]
                        {
                            "severing",
                            "scorning",
                            "freezing",
                            "poisoning",
                                                    "alienating",
                            "icing over",
                            "banishing",
                            "hardening against",
},
                        new[]
                        {
                            "the bond",
                            "the embrace",
                            "the shared memory",
                            "the family hearth",
                                                    "the old friendship",
                            "the welcoming hearth",
                            "the shared blood",
                            "the remembered home",
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
                                                    "arctic",
                            "inhuman",
                            "icebound",
                            "unrelenting",
},
                        new[]
                        {
                            "isolation",
                            "permafrost",
                            "white silence",
                            "emotional extinction",
                                                    "deep freeze",
                            "exile",
                            "dead silence",
                            "absolute estrangement",
},
                        new[]
                        {
                            "erasing",
                            "extinguishing",
                            "entombing",
                            "freezing beyond return",
                                                    "banishing beyond recall",
                            "freezing solid",
                            "silencing forever",
                            "cutting away all kinship from",
},
                        new[]
                        {
                            "the final embrace",
                            "the living heart",
                            "the human bond",
                            "every familiar voice",
                                                    "the last warm hand",
                            "the final homecoming",
                            "every human tie",
                            "the remaining voice",
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
                                                    "neighborly",
                            "close",
                            "warmhearted",
                            "kindly",
},
                        new[]
                        {
                            "kinship",
                            "home",
                            "company",
                            "nearness",
                                                    "companionship",
                            "comfort",
                            "familiarity",
                            "belonging",
},
                        new[]
                        {
                            "gathering",
                            "recognizing",
                            "tending",
                            "holding close",
                                                    "welcoming",
                            "befriending",
                            "sitting beside",
                            "making room for",
},
                        new[]
                        {
                            "the shared table",
                            "the familiar hand",
                            "the household",
                            "the returning kin",
                                                    "the homeward guest",
                            "the known face",
                            "the neighbor",
                            "the waiting family",
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
                                                    "inviting",
                            "friendly",
                            "reassuring",
                            "kindhearted",
},
                        new[]
                        {
                            "welcome",
                            "shelter",
                            "fellowship",
                            "invitation",
                                                    "hospitality",
                            "friendship",
                            "comfort",
                            "reunion",
},
                        new[]
                        {
                            "receiving",
                            "inviting",
                            "comforting",
                            "opening to",
                                                    "welcoming",
                            "reassuring",
                            "bringing inside",
                            "making room for",
},
                        new[]
                        {
                            "the stranger",
                            "the exile",
                            "the returning voice",
                            "the one outside",
                                                    "the wanderer",
                            "the lonely guest",
                            "the familiar stranger",
                            "the one at the threshold",
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
                                                    "loving",
                            "devoted",
                            "close-knit",
                            "warm-blooded",
},
                        new[]
                        {
                            "embrace",
                            "affection",
                            "union",
                            "closeness",
                                                    "devotion",
                            "reunion",
                            "tenderness",
                            "belonging",
},
                        new[]
                        {
                            "embracing",
                            "reconciling",
                            "drawing near",
                            "sheltering",
                                                    "reuniting",
                            "holding",
                            "drawing home",
                            "gathering around",
},
                        new[]
                        {
                            "the abandoned one",
                            "the estranged heart",
                            "the cold hand",
                            "the empty room",
                                                    "the forsaken one",
                            "the divided household",
                            "the estranged kin",
                            "the lonely child",
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
                                                    "rapturous",
                            "all-embracing",
                            "ardent",
                            "overwhelming",
},
                        new[]
                        {
                            "fervour",
                            "communion",
                            "ardour",
                            "devotion",
                                                    "rapture",
                            "fellowship",
                            "passion",
                            "total communion",
},
                        new[]
                        {
                            "igniting",
                            "enveloping",
                            "binding",
                            "consuming in affection",
                                                    "embracing without limit",
                            "joining",
                            "melting every distance around",
                            "drawing all into",
},
                        new[]
                        {
                            "the frozen heart",
                            "the final distance",
                            "all separation",
                            "the last lonely voice",
                                                    "the last exile",
                            "the deepest solitude",
                            "every divided heart",
                            "the final stranger",
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
                                                    "scratchy",
                            "ragged",
                            "unvarnished",
                            "blunt",
},
                        new[]
                        {
                            "grit",
                            "abrasion",
                            "splinter",
                            "noise",
                                                    "roughness",
                            "scratch",
                            "raggedness",
                            "clatter",
},
                        new[]
                        {
                            "scraping",
                            "scuffing",
                            "dragging",
                            "leaving unfinished",
                                                    "scoring",
                            "roughening",
                            "jarring",
                            "leaving ragged",
},
                        new[]
                        {
                            "the rough edge",
                            "the cracked surface",
                            "the splintered form",
                            "an uneven voice",
                                                    "the chipped edge",
                            "the scuffed surface",
                            "the crooked line",
                            "the rough-hewn form",
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
                                                    "abrasive",
                            "ragged",
                            "shoddy",
                            "overdriven",
},
                        new[]
                        {
                            "distortion",
                            "feedback",
                            "fracture",
                            "static",
                                                    "abrasion",
                            "fuzz",
                            "breakage",
                            "hiss",
},
                        new[]
                        {
                            "tearing",
                            "distorting",
                            "cracking",
                            "wrenching apart",
                                                    "overdriving",
                            "splintering",
                            "fraying",
                            "jagging",
},
                        new[]
                        {
                            "the broken rhythm",
                            "the exposed wire",
                            "the unfinished shape",
                            "the fractured signal",
                                                    "the clipped waveform",
                            "the loose string",
                            "the ragged take",
                            "the buzzing circuit",
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
                                                    "archaic",
                            "bestial",
                            "hammered",
                            "untamed",
},
                        new[]
                        {
                            "instinct",
                            "hammering",
                            "rubble",
                            "animal noise",
                                                    "clubbing",
                            "wreckage",
                            "rawhide",
                            "howl",
},
                        new[]
                        {
                            "bludgeoning",
                            "hacking",
                            "reducing",
                            "beating into shape",
                                                    "mauling",
                            "chopping",
                            "hammering",
                            "gnawing into",
},
                        new[]
                        {
                            "the carved bone",
                            "the crude weapon",
                            "the beaten frame",
                            "the primal pulse",
                                                    "the splintered club",
                            "the rough stone",
                            "the battered shell",
                            "the instinctive beat",
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
                                                    "ferocious",
                            "wrecked",
                            "eviscerated",
                            "uncontrolled",
},
                        new[]
                        {
                            "brutality",
                            "rupture",
                            "carnage",
                            "total noise",
                                                    "havoc",
                            "wreckage",
                            "mutilation",
                            "sonic collapse",
},
                        new[]
                        {
                            "pulverizing",
                            "mutilating",
                            "shattering",
                            "destroying beyond repair",
                                                    "demolishing",
                            "flaying",
                            "ripping open",
                            "reducing to wreckage",
},
                        new[]
                        {
                            "the ruined form",
                            "the bleeding signal",
                            "the crushed instrument",
                            "all remaining structure",
                                                    "the mangled frame",
                            "the shredded signal",
                            "the smashed mechanism",
                            "the last intact pattern",
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
                                                    "smooth",
                            "even",
                            "ordered",
                            "finished",
},
                        new[]
                        {
                            "polish",
                            "balance",
                            "form",
                            "clarity",
                                                    "finish",
                            "symmetry",
                            "order",
                            "definition",
},
                        new[]
                        {
                            "smoothing",
                            "shaping",
                            "balancing",
                            "clearing",
                                                    "evening",
                            "finishing",
                            "ordering",
                            "clarifying",
},
                        new[]
                        {
                            "the clean edge",
                            "the measured phrase",
                            "the shaped surface",
                            "the balanced form",
                                                    "the smooth contour",
                            "the even measure",
                            "the finished edge",
                            "the clarified phrase",
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
                                                    "sleek",
                            "methodical",
                            "composed",
                            "well-tempered",
},
                        new[]
                        {
                            "refinement",
                            "discipline",
                            "craft",
                            "control",
                                                    "technique",
                            "method",
                            "restraint",
                            "arrangement",
},
                        new[]
                        {
                            "refining",
                            "tempering",
                            "arranging",
                            "correcting",
                                                    "disciplining",
                            "composing",
                            "trimming",
                            "setting in order",
},
                        new[]
                        {
                            "the sharpened line",
                            "the disciplined hand",
                            "the ordered rhythm",
                            "the tempered blade",
                                                    "the practiced gesture",
                            "the clean articulation",
                            "the trimmed excess",
                            "the arranged passage",
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
                                                    "razor-sharp",
                            "clinical",
                            "mathematical",
                            "engineered",
},
                        new[]
                        {
                            "precision",
                            "geometry",
                            "incision",
                            "measure",
                                                    "accuracy",
                            "symmetry",
                            "calculation",
                            "tolerance",
},
                        new[]
                        {
                            "cutting",
                            "aligning",
                            "calibrating",
                            "dissecting",
                                                    "measuring",
                            "machining",
                            "sectioning",
                            "setting to tolerance",
},
                        new[]
                        {
                            "the exact wound",
                            "the measured cut",
                            "the calibrated pulse",
                            "the geometric form",
                                                    "the precise incision",
                            "the aligned mechanism",
                            "the counted interval",
                            "the engineered contour",
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
                                                    "virtuosic",
                            "immaculate",
                            "faultless",
                            "consummate",
},
                        new[]
                        {
                            "mastery",
                            "perfection",
                            "command",
                            "immaculate form",
                                                    "virtuosity",
                            "completion",
                            "supremacy",
                            "perfect order",
},
                        new[]
                        {
                            "perfecting",
                            "commanding",
                            "carving without error",
                            "reducing to exactness",
                                                    "mastering",
                            "finishing beyond revision",
                            "executing flawlessly",
                            "bringing to perfection",
},
                        new[]
                        {
                            "the final form",
                            "the flawless edge",
                            "the completed design",
                            "absolute control",
                                                    "the immaculate mechanism",
                            "the faultless sequence",
                            "the unsurpassable form",
                            "the work without defect",
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
                                                    "hasty",
                            "swift",
                            "quickened",
                            "breathless",
},
                        new[]
                        {
                            "haste",
                            "alarm",
                            "impulse",
                            "departure",
                                                    "rush",
                            "warning",
                            "flight",
                            "restlessness",
},
                        new[]
                        {
                            "hurrying",
                            "calling forth",
                            "awakening",
                            "pressing onward",
                                                    "rushing toward",
                            "spurring",
                            "hastening",
                            "setting in motion",
},
                        new[]
                        {
                            "the sudden warning",
                            "the open road",
                            "the restless step",
                            "the approaching hour",
                                                    "the next alarm",
                            "the departing train",
                            "the hurried footstep",
                            "the open gate",
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
                                                    "speeding",
                            "headlong",
                            "charging",
                            "insistent",
},
                        new[]
                        {
                            "momentum",
                            "pursuit",
                            "charge",
                            "acceleration",
                                                    "speed",
                            "chase",
                            "surge",
                            "drive",
},
                        new[]
                        {
                            "pursuing",
                            "overtaking",
                            "driving through",
                            "forcing onward",
                                                    "chasing",
                            "surging past",
                            "pressing",
                            "accelerating toward",
},
                        new[]
                        {
                            "the fleeing figure",
                            "the turning wheel",
                            "the closing distance",
                            "the racing pulse",
                                                    "the retreating shape",
                            "the spinning axle",
                            "the narrowing gap",
                            "the hammering heart",
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
                                                    "manic",
                            "raging",
                            "breakneck",
                            "runaway",
},
                        new[]
                        {
                            "frenzy",
                            "stampede",
                            "panic",
                            "eruption",
                                                    "rush",
                            "maelstrom",
                            "onslaught",
                            "overdrive",
},
                        new[]
                        {
                            "scattering",
                            "surging through",
                            "tearing past",
                            "outrunning",
                                                    "stampeding through",
                            "ripping through",
                            "careening past",
                            "breaking formation around",
},
                        new[]
                        {
                            "the broken formation",
                            "the collapsing procession",
                            "the panicked crowd",
                            "the failing rhythm",
                                                    "the scattered ranks",
                            "the buckling line",
                            "the terrified host",
                            "the ruptured meter",
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
                                                    "searing",
                            "detonating",
                            "runaway",
                            "cataclysmic",
},
                        new[]
                        {
                            "detonation",
                            "firestorm",
                            "velocity",
                            "total acceleration",
                                                    "blast",
                            "conflagration",
                            "supersonic rush",
                            "instant ruin",
},
                        new[]
                        {
                            "incinerating",
                            "crossing in an instant",
                            "obliterating in passage",
                            "burning beyond control",
                                                    "blasting through",
                            "erasing in a flash",
                            "detonating across",
                            "leaving nothing behind in",
},
                        new[]
                        {
                            "the final barrier",
                            "all remaining distance",
                            "the last measured breath",
                            "the world left behind",
                                                    "the final checkpoint",
                            "the remaining horizon",
                            "the last counted second",
                            "everything unable to flee",
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
                                                    "unhurried",
                            "steady",
                            "methodical",
                            "lingering",
},
                        new[]
                        {
                            "patience",
                            "interval",
                            "procession",
                            "pause",
                                                    "pace",
                            "waiting",
                            "cadence",
                            "delay",
},
                        new[]
                        {
                            "waiting",
                            "pacing",
                            "counting",
                            "approaching carefully",
                                                    "lingering",
                            "advancing slowly toward",
                            "measuring out",
                            "holding back",
},
                        new[]
                        {
                            "the measured step",
                            "the patient hand",
                            "the long road",
                            "the waiting hour",
                                                    "the steady footfall",
                            "the waiting gate",
                            "the winding road",
                            "the long afternoon",
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
                                                    "leaden",
                            "plodding",
                            "laboured",
                            "weighted",
},
                        new[]
                        {
                            "burden",
                            "weight",
                            "march",
                            "pressure",
                                                    "heaviness",
                            "trudge",
                            "drag",
                            "load",
},
                        new[]
                        {
                            "burdening",
                            "weighing down",
                            "slowing",
                            "pressing upon",
                                                    "dragging down",
                            "loading",
                            "retarding",
                            "making heavy",
},
                        new[]
                        {
                            "the bent back",
                            "the iron step",
                            "the burdened road",
                            "the sinking body",
                                                    "the stooped shoulder",
                            "the leaden boot",
                            "the uphill path",
                            "the tiring frame",
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
                                                    "laboured",
                            "drawn-out",
                            "wearing",
                            "slow-grinding",
},
                        new[]
                        {
                            "delay",
                            "exhaustion",
                            "attrition",
                            "endless passage",
                                                    "weariness",
                            "erosion",
                            "prolongation",
                            "slow ruin",
},
                        new[]
                        {
                            "dragging",
                            "restraining",
                            "grinding down",
                            "drawing out",
                                                    "wearing down",
                            "holding back",
                            "prolonging",
                            "stretching beyond",
},
                        new[]
                        {
                            "the wounded procession",
                            "the final mile",
                            "the exhausted breath",
                            "the unending night",
                                                    "the limping column",
                            "the last ascent",
                            "the failing lung",
                            "the night without dawn",
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
                                                    "tectonic",
                            "motionless",
                            "age-old",
                            "world-stopping",
},
                        new[]
                        {
                            "monolith",
                            "inevitability",
                            "stasis",
                            "endless weight",
                                                    "immobility",
                            "geology",
                            "duration",
                            "eternity",
},
                        new[]
                        {
                            "arresting",
                            "entombing",
                            "fixing in place",
                            "crushing beneath duration",
                                                    "pinning in place",
                            "burying under ages",
                            "slowing to nothing",
                            "holding motionless",
},
                        new[]
                        {
                            "the halted world",
                            "the buried impulse",
                            "all attempted motion",
                            "the age without end",
                                                    "the unmoving earth",
                            "the arrested charge",
                            "every attempt to flee",
                            "the century without change",
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
                                                    "cross",
                            "antagonistic",
                            "snarling",
                            "belligerent",
},
                        new[]
                        {
                            "irritation",
                            "quarrel",
                            "anger",
                            "grievance",
                                                    "hostility",
                            "snarl",
                            "resentment",
                            "provocation",
},
                        new[]
                        {
                            "snapping at",
                            "provoking",
                            "pushing away",
                            "turning against",
                                                    "berating",
                            "needling",
                            "shoving",
                            "picking at",
},
                        new[]
                        {
                            "the hostile glance",
                            "the raised voice",
                            "the petty grievance",
                            "the clenched hand",
                                                    "the sour remark",
                            "the provoking stare",
                            "the cheap accusation",
                            "the raised fist",
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
                                                    "nasty",
                            "rancorous",
                            "poisonous",
                            "mean-spirited",
},
                        new[]
                        {
                            "spite",
                            "hatred",
                            "venom",
                            "insult",
                                                    "rancour",
                            "grudge",
                            "slander",
                            "barb",
},
                        new[]
                        {
                            "insulting",
                            "humiliating",
                            "wounding",
                            "denying",
                                                    "taunting",
                            "betraying",
                            "belittling",
                            "spiting",
},
                        new[]
                        {
                            "the rival",
                            "the trusting face",
                            "the offered peace",
                            "the wounded pride",
                                                    "the old rival",
                            "the open-hearted fool",
                            "the truce",
                            "the bruised ego",
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
                                                    "retaliatory",
                            "revengeful",
                            "retributive",
                            "bloodthirsty",
},
                        new[]
                        {
                            "vengeance",
                            "retaliation",
                            "punishment",
                            "blood-debt",
                                                    "retribution",
                            "reprisal",
                            "reckoning",
                            "vendetta",
},
                        new[]
                        {
                            "avenging",
                            "hunting down",
                            "punishing",
                            "repaying in blood",
                                                    "retaliating against",
                            "pursuing",
                            "settling accounts with",
                            "collecting blood from",
},
                        new[]
                        {
                            "the guilty one",
                            "the former friend",
                            "the remembered injury",
                            "the pleading enemy",
                                                    "the betrayer",
                            "the old companion",
                            "the unpaid wrong",
                            "the cornered foe",
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
                                                    "vicious",
                            "pitiless",
                            "torturous",
                            "destructive",
},
                        new[]
                        {
                            "malice",
                            "cruelty",
                            "torment",
                            "deliberate ruin",
                                                    "sadism",
                            "savagery",
                            "agony",
                            "wanton destruction",
},
                        new[]
                        {
                            "torturing",
                            "destroying",
                            "corrupting",
                            "harming for pleasure",
                                                    "mutilating",
                            "tormenting",
                            "ruining",
                            "breaking for amusement",
},
                        new[]
                        {
                            "the helpless",
                            "the innocent",
                            "the final survivor",
                            "everything still unharmed",
                                                    "the defenseless",
                            "the blameless",
                            "the captive",
                            "the last thing left whole",
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
                                                    "helpful",
                            "humane",
                            "caring",
                            "forgiving",
},
                        new[]
                        {
                            "kindness",
                            "mercy",
                            "aid",
                            "goodwill",
                                                    "care",
                            "help",
                            "pity",
                            "comfort",
},
                        new[]
                        {
                            "helping",
                            "tending",
                            "forgiving",
                            "easing",
                                                    "assisting",
                            "comforting",
                            "pardoning",
                            "mending",
},
                        new[]
                        {
                            "the injured stranger",
                            "the tired hand",
                            "the minor wound",
                            "the one in need",
                                                    "the hurt passerby",
                            "the weary arm",
                            "the small injury",
                            "the troubled neighbor",
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
                                                    "openhanded",
                            "altruistic",
                            "bountiful",
                            "magnanimous",
},
                        new[]
                        {
                            "generosity",
                            "offering",
                            "relief",
                            "gift",
                                                    "charity",
                            "alms",
                            "support",
                            "provision",
},
                        new[]
                        {
                            "giving to",
                            "feeding",
                            "sharing with",
                            "relieving",
                                                    "supplying",
                            "clothing",
                            "providing for",
                            "supporting",
},
                        new[]
                        {
                            "the hungry",
                            "the dispossessed",
                            "the empty hand",
                            "the abandoned household",
                                                    "the starving",
                            "the homeless",
                            "the begging hand",
                            "the ruined family",
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
                                                    "guardian",
                            "watchful",
                            "defensive",
                            "unyielding",
},
                        new[]
                        {
                            "protection",
                            "shelter",
                            "guardianship",
                            "defence",
                                                    "guard",
                            "refuge",
                            "ward",
                            "rescue",
},
                        new[]
                        {
                            "defending",
                            "shielding",
                            "rescuing",
                            "standing before",
                                                    "guarding",
                            "covering",
                            "saving",
                            "interposing for",
},
                        new[]
                        {
                            "the threatened child",
                            "the wounded kin",
                            "the hunted one",
                            "the vulnerable",
                                                    "the endangered child",
                            "the injured family",
                            "the fugitive",
                            "the defenseless",
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
                                                    "redemptive",
                            "self-sacrificing",
                            "salvific",
                            "self-effacing",
},
                        new[]
                        {
                            "compassion",
                            "salvation",
                            "sacrifice",
                            "deliverance",
                                                    "redemption",
                            "martyrdom",
                            "grace",
                            "rescue without limit",
},
                        new[]
                        {
                            "sacrificing for",
                            "redeeming",
                            "preserving at any cost",
                            "suffering in place of",
                                                    "dying for",
                            "absolving",
                            "giving everything for",
                            "taking the punishment of",
},
                        new[]
                        {
                            "the condemned",
                            "the enemy",
                            "every wounded life",
                            "those beyond forgiveness",
                                                    "the doomed",
                            "the persecutor",
                            "all suffering life",
                            "the unforgivable",
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
                                                    "somber",
                            "fading",
                            "elegiac",
                            "ashen",
},
                        new[]
                        {
                            "gloom",
                            "sorrow",
                            "dusk",
                            "decline",
                                                    "mourning",
                            "twilight",
                            "lament",
                            "waning",
},
                        new[]
                        {
                            "mourning",
                            "dimming",
                            "withering",
                            "lingering over",
                                                    "lamenting",
                            "fading beside",
                            "watching die",
                            "dwelling beside",
},
                        new[]
                        {
                            "the fading light",
                            "the empty bed",
                            "the dying day",
                            "the quiet grave",
                                                    "the last candle",
                            "the vacant pillow",
                            "the setting sun",
                            "the fresh earth",
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
                                                    "ghastly",
                            "corpse-pale",
                            "mortuary",
                            "gruesome",
},
                        new[]
                        {
                            "corpse",
                            "remains",
                            "pallor",
                            "decay",
                                                    "cadaver",
                            "shroud",
                            "putrefaction",
                            "mortality",
},
                        new[]
                        {
                            "exposing",
                            "exhuming",
                            "rotting",
                            "arranging for burial",
                                                    "uncovering",
                            "disinterring",
                            "putrefying",
                            "laying out",
},
                        new[]
                        {
                            "the opened grave",
                            "the pale body",
                            "the funeral cloth",
                            "the waiting coffin",
                                                    "the shallow grave",
                            "the lifeless face",
                            "the burial shroud",
                            "the open casket",
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
                                                    "mourning",
                            "gravebound",
                            "coffin-dark",
                            "interred",
},
                        new[]
                        {
                            "funeral",
                            "burial",
                            "tomb",
                            "procession",
                                                    "obsequy",
                            "interment",
                            "crypt",
                            "death march",
},
                        new[]
                        {
                            "burying",
                            "embalming",
                            "entombing",
                            "carrying to the grave",
                                                    "interring",
                            "laying out",
                            "sealing in a crypt",
                            "leading to burial",
},
                        new[]
                        {
                            "the final breath",
                            "the funeral host",
                            "the buried name",
                            "the sealed crypt",
                                                    "the dying breath",
                            "the mourning line",
                            "the forgotten corpse",
                            "the locked tomb",
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
                                                    "charnel",
                            "death-saturated",
                            "bone-white",
                            "extinction-bound",
},
                        new[]
                        {
                            "sepulchre",
                            "charnel house",
                            "extinction",
                            "eternal burial",
                                                    "necrosis",
                            "ossuary",
                            "mass grave",
                            "death without end",
},
                        new[]
                        {
                            "reducing to remains",
                            "sealing beneath stone",
                            "extinguishing",
                            "returning everything to dust",
                                                    "rendering lifeless",
                            "burying beyond recovery",
                            "ending every pulse",
                            "turning the world to bone",
},
                        new[]
                        {
                            "the last living body",
                            "every beating heart",
                            "the unburied world",
                            "all remaining life",
                                                    "the final warm body",
                            "every living pulse",
                            "the breathing earth",
                            "the remainder of life",
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
                                                    "living",
                            "quick",
                            "renewed",
                            "stirring",
},
                        new[]
                        {
                            "breath",
                            "pulse",
                            "awakening",
                            "motion",
                                                    "life",
                            "respiration",
                            "stirring",
                            "wakefulness",
},
                        new[]
                        {
                            "breathing into",
                            "stirring",
                            "awakening",
                            "moving through",
                                                    "enlivening",
                            "rousing",
                            "kindling life in",
                            "coursing through",
},
                        new[]
                        {
                            "the open air",
                            "the waking body",
                            "the first pulse",
                            "the living hand",
                                                    "the morning air",
                            "the stirring flesh",
                            "the returning beat",
                            "the grasping hand",
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
                                                    "animated",
                            "robust",
                            "quickened",
                            "pulsing",
},
                        new[]
                        {
                            "energy",
                            "heartbeat",
                            "current",
                            "movement",
                                                    "vigour",
                            "pulsebeat",
                            "flow",
                            "activity",
},
                        new[]
                        {
                            "reviving",
                            "quickening",
                            "driving",
                            "raising",
                                                    "reanimating",
                            "energizing",
                            "setting in motion",
                            "lifting",
},
                        new[]
                        {
                            "the exhausted body",
                            "the dormant seed",
                            "the weakened heart",
                            "the fallen one",
                                                    "the spent body",
                            "the sleeping bulb",
                            "the fainting heart",
                            "the collapsed figure",
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
                                                    "verdant",
                            "fecund",
                            "blooming",
                            "regenerative",
},
                        new[]
                        {
                            "growth",
                            "renewal",
                            "abundance",
                            "regeneration",
                                                    "fertility",
                            "rebirth",
                            "plenty",
                            "recovery",
},
                        new[]
                        {
                            "restoring",
                            "blooming through",
                            "regenerating",
                            "multiplying within",
                                                    "renewing",
                            "sprouting through",
                            "healing",
                            "spreading through",
},
                        new[]
                        {
                            "the ruined field",
                            "the wounded flesh",
                            "the barren ground",
                            "the broken lineage",
                                                    "the scorched field",
                            "the torn body",
                            "the sterile soil",
                            "the severed bloodline",
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
                                                    "teeming",
                            "prolific",
                            "unstoppable",
                            "life-swollen",
},
                        new[]
                        {
                            "vitality",
                            "profusion",
                            "eruption",
                            "living abundance",
                                                    "fecundity",
                            "overgrowth",
                            "rebirth",
                            "unchecked life",
},
                        new[]
                        {
                            "overwhelming",
                            "bursting from",
                            "resurrecting",
                            "consuming through growth",
                                                    "flooding with life",
                            "breaking out of",
                            "bringing back from death",
                            "overrunning",
},
                        new[]
                        {
                            "the sealed tomb",
                            "the final extinction",
                            "every barren place",
                            "the dominion of death",
                                                    "the locked grave",
                            "the last death",
                            "every dead field",
                            "the kingdom of decay",
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
                                                    "blank",
                            "unmoored",
                            "pointless",
                            "unmarked",
},
                        new[]
                        {
                            "emptiness",
                            "absence",
                            "vacancy",
                            "echo",
                                                    "blankness",
                            "silence",
                            "drift",
                            "vacuum",
},
                        new[]
                        {
                            "emptying",
                            "abandoning",
                            "overlooking",
                            "stripping significance from",
                                                    "hollowing",
                            "discarding",
                            "passing over",
                            "removing significance from",
},
                        new[]
                        {
                            "the unanswered question",
                            "the vacant sign",
                            "the forgotten name",
                            "the aimless road",
                                                    "the unasked question",
                            "the blank emblem",
                            "the erased signature",
                            "the road without destination",
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
                                                    "absurd",
                            "pointless",
                            "null",
                            "disenchanted",
},
                        new[]
                        {
                            "futility",
                            "negation",
                            "nothingness",
                            "denial",
                                                    "absurdity",
                            "nullity",
                            "pointlessness",
                            "disbelief",
},
                        new[]
                        {
                            "denying",
                            "negating",
                            "dismissing",
                            "erasing meaning from",
                                                    "nullifying",
                            "ridiculing",
                            "discarding",
                            "stripping purpose from",
},
                        new[]
                        {
                            "the promise",
                            "the cause",
                            "the oath",
                            "the chosen direction",
                                                    "the pledge",
                            "the movement",
                            "the sworn word",
                            "the intended destination",
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
                                                    "despairing",
                            "forsaken",
                            "wasted",
                            "end-stage",
},
                        new[]
                        {
                            "despair",
                            "desolation",
                            "ruin",
                            "final doubt",
                                                    "hopelessness",
                            "wasteland",
                            "collapse",
                            "last uncertainty",
},
                        new[]
                        {
                            "hollowing out",
                            "extinguishing hope in",
                            "reducing to futility",
                            "abandoning beyond return",
                                                    "emptying",
                            "killing hope in",
                            "making pointless",
                            "discarding forever",
},
                        new[]
                        {
                            "the guiding star",
                            "the final hope",
                            "the last reason",
                            "the imagined future",
                                                    "the north star",
                            "the remaining hope",
                            "the final motive",
                            "the future once imagined",
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
                                                    "null",
                            "voidbound",
                            "world-erasing",
                            "terminally empty",
},
                        new[]
                        {
                            "abyss",
                            "oblivion",
                            "nonbeing",
                            "total negation",
                                                    "nullity",
                            "nonexistence",
                            "erasure",
                            "absolute nothing",
},
                        new[]
                        {
                            "devouring purpose",
                            "unmaking meaning",
                            "erasing every name",
                            "returning all to nothing",
                                                    "consuming all purpose in",
                            "deleting meaning from",
                            "obliterating every name from",
                            "collapsing into nothing",
},
                        new[]
                        {
                            "the final answer",
                            "every remembered name",
                            "all purpose",
                            "the world of signs",
                                                    "the ultimate answer",
                            "every surviving memory",
                            "all intention",
                            "the entire symbolic world",
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
                                                    "intentional",
                            "oriented",
                            "chosen",
                            "meaningful",
},
                        new[]
                        {
                            "purpose",
                            "direction",
                            "task",
                            "sign",
                                                    "intent",
                            "aim",
                            "calling",
                            "mark",
},
                        new[]
                        {
                            "assigning",
                            "naming",
                            "guiding",
                            "giving direction to",
                                                    "appointing",
                            "designating",
                            "directing",
                            "setting a purpose for",
},
                        new[]
                        {
                            "the chosen path",
                            "the marked stone",
                            "the answered question",
                            "the deliberate act",
                                                    "the intended road",
                            "the inscribed marker",
                            "the resolved question",
                            "the conscious deed",
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
                                                    "dutiful",
                            "sworn",
                            "loyal",
                            "purpose-bound",
},
                        new[]
                        {
                            "devotion",
                            "duty",
                            "vow",
                            "cause",
                                                    "commitment",
                            "obligation",
                            "pledge",
                            "vocation",
},
                        new[]
                        {
                            "dedicating",
                            "binding to purpose",
                            "serving",
                            "keeping faith with",
                                                    "pledging",
                            "committing to",
                            "upholding",
                            "answering the call of",
},
                        new[]
                        {
                            "the oath",
                            "the chosen cause",
                            "the entrusted task",
                            "the promised destination",
                                                    "the covenant",
                            "the appointed cause",
                            "the assigned duty",
                            "the promised end",
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
                                                    "doctrinal",
                            "driven",
                            "unyielding",
                            "fate-bound",
},
                        new[]
                        {
                            "mission",
                            "conviction",
                            "mandate",
                            "destiny",
                                                    "purpose",
                            "creed",
                            "imperative",
                            "fate",
},
                        new[]
                        {
                            "proclaiming",
                            "pursuing without rest",
                            "sacrificing to",
                            "forcing into purpose",
                                                    "declaring",
                            "chasing to the end",
                            "offering everything to",
                            "ordering around a purpose",
},
                        new[]
                        {
                            "the final cause",
                            "the revealed doctrine",
                            "the ordained path",
                            "the absolute demand",
                                                    "the governing cause",
                            "the declared creed",
                            "the destined road",
                            "the binding command",
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
                                                    "epochal",
                            "visionary",
                            "cosmic",
                            "all-defining",
},
                        new[]
                        {
                            "revelation",
                            "destiny",
                            "cosmic order",
                            "final meaning",
                                                    "epiphany",
                            "providence",
                            "world-order",
                            "ultimate purpose",
},
                        new[]
                        {
                            "transfiguring",
                            "revealing",
                            "naming the world",
                            "making eternal",
                                                    "recasting",
                            "unveiling",
                            "giving the world a name",
                            "engraving into eternity",
},
                        new[]
                        {
                            "the last question",
                            "mortal existence",
                            "the silent universe",
                            "all remaining doubt",
                                                    "the final mystery",
                            "human life",
                            "the mute cosmos",
                            "every surviving uncertainty",
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