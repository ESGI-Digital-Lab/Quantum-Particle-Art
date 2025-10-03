using System;
using System.Linq;
using NaughtyAttributes;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Assertions;

[CreateAssetMenu(menuName = "Particle/InitConditions", fileName = "InitConditions", order = 0)]
public class RulesSaved : ScriptableObject
{
    [SerializeField] private Ruleset _rules;
    public Ruleset Rules => _rules;

    public RulesSaved(int nb, Defaults type)
    {
        _locked = false;
        _nbSpecies = nb;
        Assert.IsTrue(type != Defaults.Default, "Default rules are purely internal and don't do nothing");
        _rules = Load(type);
    }

    public void ReplaceRules(Ruleset rules)
    {
        _rules = rules;
    }

    public enum Defaults
    {
        Default = 0,
        Random = 1,
        Alliances = 2,
        Ships = 3,
        Purple = 4,
        Simplify = 5,
        NoFriction = 6,
        Field = 7,
        Menace = 8
    }

    [HorizontalLine(10)] [Range(2, 100)] [SerializeField] [Header("Pre built rules")]
    private int _nbSpecies = 12;

    [Button]
    public void LoadRandom() => LoadDefault(Defaults.Random, true);

    [Button]
    public void LoadRandomNoStart() => LoadDefault(Defaults.Random, false);

    [Button]
    public void LoadAlliances() => LoadDefault(Defaults.Alliances);

    //[Button] private void LoadShips() => LoadDefault(Defaults.Ships);

    [Button, Tooltip("Will override species count to 2")]
    public void LoadStigmergy() => LoadDefault(Defaults.Purple);

    //[Button, Tooltip("Basically empty interactions")] private void LoadDefault() => LoadDefault(Defaults.Default);

    [Button]
    public void LoadSimplify() => LoadDefault(Defaults.Simplify);

    [SerializeField, ReadOnly] private bool _locked = false;

    [Button, ShowIf(nameof(_locked))]
    public void Unlock() => _locked = false;

    [Button, HideIf(nameof(_locked))]
    public void Lock() => _locked = true;

    public void LoadDefault(Defaults _typeOfRuleset, bool autoStart = false)
    {
        if (_locked)
        {
#if UNITY_EDITOR
            bool overwrite = EditorUtility.DisplayDialog(
                "Overwrite locked rules ?",
                "Rules are currently locked (probably after they've been defined by another default). Do you really want to overwrite ? It's better to create a copy or a new instance, unlock it and load it into this other one.",
                "Yes, overwrite", "Cancel"
            );
            if (!overwrite)
                return;
#endif
        }

        ReplaceRules(Load(_typeOfRuleset));
#if UNITY_EDITOR
        if (autoStart)
            EditorApplication.EnterPlaymode();
        else
#endif
        _locked = true;
    }

    private Ruleset Load(Defaults target)
    {
        switch (target)
        {
            case Defaults.Default:
                return CreateDefault();
            case Defaults.Random:
                return CreateRandom();
            case Defaults.Alliances:
                return CreateAlliances();
            case Defaults.Ships:
                return CreateShips();
            case Defaults.Purple:
                return CreatePurple();
            case Defaults.Simplify:
                return CreateSimplify();
            case Defaults.NoFriction:
                return NoInteractionNorFriction();
            case Defaults.Field:
                return CreateField();
            case Defaults.Menace:
                return CreateMenace();
            default:
                throw new ArgumentOutOfRangeException(nameof(target), target, null);
        }
    }

    private Ruleset CreateField()
    {
        _nbSpecies = 2;
        var nb = _nbSpecies;
        var species = new Ruleset.Species[nb];
        var inter0 = new Ruleset.Species.InteractionFactor[]
        {
            new Ruleset.Species.InteractionFactor(
                0.2f,
                20f,
                0.06f,
                30f
            ),
            new Ruleset.Species.InteractionFactor(
                0.2f,
                20f,
                0.2f,
                30f
            )
        };
        species[0] = new Ruleset.Species(inter0, 0, false, 0.02f, 0.02f);

        var inter1 = new Ruleset.Species.InteractionFactor[]
        {
            new Ruleset.Species.InteractionFactor(
                0.2f,
                20f,
                0.2f,
                30f
            ),
            new Ruleset.Species.InteractionFactor(
                0.2f,
                20f,
                -.4f,
                30f
            )
        };
        species[1] = new Ruleset.Species(inter1, 0, false, 0.02f, 0.01f);
        return new Ruleset(species, "Field");
    }

    private Ruleset CreateSimplify()
    {
        var nb = _nbSpecies;
        var selfCF = 0.2f;
        var selfCR = 20f;
        var selfSF = 0.5f;
        var selfSR = 200.0f;

        var otherCF = 0.5f;
        var otherCR = 80.0f;
        var otherSF = 0.3f;
        var otherSR = 300f;
        var species = new Ruleset.Species[nb];
        for (var i = 0; i < nb; i++)
        {
            var interactions = new Ruleset.Species.InteractionFactor[nb];
            for (var j = 0; j < nb; j++)
            {
                if (i == j)
                {
                    interactions[j] = new Ruleset.Species.InteractionFactor(
                        selfCF,
                        selfCR,
                        selfSF,
                        selfSR
                    );
                }
                else
                {
                    interactions[j] = new Ruleset.Species.InteractionFactor(
                        otherCF,
                        otherCR,
                        otherSF,
                        otherSR
                    );
                }
            }

            species[i] = new Ruleset.Species(interactions, 0);
        }

        return new Ruleset(species, "Simplify_" + nb);
    }

    private Ruleset CreatePurple()
    {
        _nbSpecies = 2;
        var nb = _nbSpecies;
        var species = new Ruleset.Species[nb];
        var inter0 = new Ruleset.Species.InteractionFactor[]
        {
            new Ruleset.Species.InteractionFactor(
                0.2456388529151845f,
                3.1298788652029006f,
                2.7742121784826965f,
                61.84255798045096f
            ),
            new Ruleset.Species.InteractionFactor(
                1.8847939251882317f,
                11.466280325834706f,
                -0.5593978530532642f,
                53.29011192416681f
            )
        };
        species[0] = new Ruleset.Species(inter0, 0);

        var inter1 = new Ruleset.Species.InteractionFactor[]
        {
            new Ruleset.Species.InteractionFactor(
                1.360127701275812f,
                6.283256392503164f,
                3.2967553169706694f,
                77.38057989345845f
            ),
            new Ruleset.Species.InteractionFactor(
                1.8229897992337158f,
                5.847848852306933f,
                -3.748120264414312f,
                6.188722840850666f
            )
        };
        species[1] = new Ruleset.Species(inter1, 2);
        return new Ruleset(species, "Stigmergy");
    }

    private Ruleset CreateShips()
    {
        throw new NotImplementedException();
    }

    private Ruleset CreateMenace()
    {
        var nb = _nbSpecies;
        var interactions = new Ruleset.Species.InteractionFactor[nb][];
        var defaultInter = new Ruleset.Species.InteractionFactor(1.0f, 8.64f, 4 * .1f, 81.0f * .2f);
        for (int i = 0; i < nb; i++)
        {
            interactions[i] = new Ruleset.Species.InteractionFactor[nb];
            for (int j = 0; j < nb; j++)
                interactions[i][j] = defaultInter;
        }

        var fear = 4 * -1f;
        var love = 4 * 1.2f;

        var fearRadius = 81 * .5f;
        var loveRadius = 81 * .9f;
        int menaces = 1;
        for (int i = menaces; i < nb; i++)
        {
            interactions[0][i].SocialForce = love;
            interactions[0][i].SocialRadius = loveRadius;

            interactions[i][0].SocialForce = fear;
            interactions[i][0].SocialRadius = fearRadius * (i * 1f / nb);
        }

        var species = new Ruleset.Species[nb];
        for (int i = 0; i < nb; i++)
        {
            species[i] = new Ruleset.Species(interactions[i]);
        }

        return new Ruleset(species, "Alliances_" + nb);
    }

    private Ruleset CreateAlliances()
    {
        var nb = _nbSpecies;
        var interactions = new Ruleset.Species.InteractionFactor[nb][];
        var defaultInter = new Ruleset.Species.InteractionFactor(1.0f, 8.64f, -4 * .2f, 81.0f, false);
        for (int i = 0; i < nb; i++)
        {
            interactions[i] = new Ruleset.Species.InteractionFactor[nb];
            for (int j = 0; j < nb; j++)
                interactions[i][j] = defaultInter;
        }

        var me = 4 * .2f;
        var before = 4 * -.5f;
        var after = 4 * .5f;
        for (int i = 0; i < nb; i++)
        {
            var f0 = i - 1;
            if (f0 < 0)
                f0 += nb; //nb-1
            var f1 = i;
            var f2 = i + 1;
            if (f2 >= nb)
                f2 -= nb; //0
            interactions[i][f0].SocialForce = before;
            interactions[i][f1].SocialForce = me;
            interactions[i][f2].SocialForce = after;
        }

        var species = new Ruleset.Species[nb];
        for (int i = 0; i < nb; i++)
        {
            species[i] = new Ruleset.Species(interactions[i]);
        }

        return new Ruleset(species, "Alliances_" + nb);
    }

    private Ruleset CreateRandom()
    {
        var random = new System.Random(DateTime.Now.Ticks.GetHashCode());
        var stepping = .4;
        var nb = _nbSpecies;
        var species = new Ruleset.Species[nb];
        for (int i = 0; i < nb; i++)
        {
            var interactions = new Ruleset.Species.InteractionFactor[nb];
            for (int j = 0; j < nb; j++)
            {
                var collisionRadius = RandomRange(random, 0, 20);
                interactions[j] = new Ruleset.Species.InteractionFactor(
                    RandomRange(random, 0, 2),
                    collisionRadius,
                    RandomRange(random, -5f, 5f),
                    collisionRadius + RandomRange(random, 0, 100),
                    RandomRange(random, 0, 1) > .5f);
            }

            species[i] = new Ruleset.Species(
                interactions,
                RandomRange(random, 0, 1) < stepping ? Mathf.FloorToInt(RandomRange(random, 0, Ruleset.MaxSteps)) : 0,
                RandomRange(random, 0, 1) > 1 / 3f,
                0.01f,
                RandomRange(random, 0.1f, 0.4f)
            );
        }

        return new Ruleset(species, "Random_" + nb + "_" + string.Join('_', species.Select(s => s.Steps.ToString())));
    }

    private float RandomRange(System.Random random, float min, float max)
    {
        return ((float)random.NextDouble() * (max - min)) + min;
    }

    private Ruleset CreateDefault()
    {
        var interactions = new Ruleset.Species.InteractionFactor[1]
        {
            new()
        };
        var species = new Ruleset.Species[1]
        {
            new(interactions, 0, false, 0.02f, 0.2f)
        };
        return new Ruleset(species, "Default");
    }

    private Ruleset NoInteractionNorFriction()
    {
        var def = new Ruleset.Species.InteractionFactor(0, 0, 0, 0, false);
        var species = new Ruleset.Species[_nbSpecies];
        for (int i = 0; i < _nbSpecies; i++)
        {
            var interactions = new Ruleset.Species.InteractionFactor[_nbSpecies];
            for (int j = 0; j < _nbSpecies; j++)
                interactions[j] = def;
            species[i] = new Ruleset.Species(interactions, 0, false, 0f, 0f);
        }

        return new Ruleset(species, "NoInteractionNorFriction_" + _nbSpecies);
    }
}