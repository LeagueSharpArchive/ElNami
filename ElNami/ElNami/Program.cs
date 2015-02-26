using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;

namespace ElNami
{
    internal class Program
    {
        private const string hero = "Nami";

        private static Obj_AI_Hero Player
        {
            get { return ObjectManager.Player; }
        }

        private static Menu _menu;
        private static Orbwalking.Orbwalker _orbwalker;
        private static Spell _q, _w, _e, _r;
        private static readonly List<Spell> _spellList = new List<Spell>();
        private static SpellSlot _ignite;

        #region Main

        private static void Main(string[] args)
        {
            LeagueSharp.Common.CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        #endregion

        #region Gameloaded 

        private static void Game_OnGameLoad(EventArgs args)
        {
            if (!Player.ChampionName.Equals(hero, StringComparison.CurrentCultureIgnoreCase))
            {
                return;
            }
            Notifications.AddNotification("ElNami by jQuery v1.0.0.2", 10000);

            #region Spell Data

            // Initialize spells
            _q = new Spell(SpellSlot.Q, 875);
            _w = new Spell(SpellSlot.W, 725);
            _e = new Spell(SpellSlot.E, 800);
            _r = new Spell(SpellSlot.R, 2750);

            // Add to spell list
            _spellList.AddRange(new[] { _q, _w, _e, _r });

            // Initialize ignite
            _ignite = Player.GetSpellSlot("summonerdot");

            #endregion

            //Event handlers
            Game.OnGameUpdate += Game_OnGameUpdate;
            _q.SetSkillshot(1f, 150f, float.MaxValue, false, SkillshotType.SkillshotCircle);
            _r.SetSkillshot(0.5f, 260f, 850f, false, SkillshotType.SkillshotLine);
            Interrupter2.OnInterruptableTarget += Interrupter2_OnInterruptableTarget;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;

            try
            {
                InitializeMenu();
            }
            catch (Exception ex) {}
        }

        #endregion

        #region OnGameUpdate

        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (_menu.Item("ComboActive").GetValue<KeyBind>().Active)
            {
                Combo();
            }

            if (_orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
            {
                Harass();
            }

            var target = TargetSelector.GetTarget(_w.Range, TargetSelector.DamageType.Physical);
            if (Interrupter2.IsCastingInterruptableSpell(target) &&
                Interrupter2.GetInterruptableTargetData(target).DangerLevel == Interrupter2.DangerLevel.High &&
                target.IsValidTarget(_q.Range))
            {
                _q.Cast();
            }

            HealSelf();
            HealAlly();
        }


        #region Healself()

        private static void HealSelf()
        {

            if (Player.HasBuff("Recall") || Utility.InFountain(Player))
            {
                return;
            }
            if (_menu.Item("SelfHeal").GetValue<bool>() &&
                (Player.Health / Player.MaxHealth) * 100 <= _menu.Item("SelfHperc").GetValue<Slider>().Value &&
                Player.ManaPercentage() >= _menu.Item("minmanaE").GetValue<Slider>().Value && _w.IsReady())
            {
                _w.Cast(Player);
            }
        }

        #endregion
    
        #endregion

        #region HealAlly()

        private static void HealAlly()
        {
            foreach (var hero in ObjectManager.Get<Obj_AI_Hero>().Where(h => h.IsAlly && !h.IsMe))
            {
                if (Player.HasBuff("Recall") || Utility.InFountain(Player))
                {
                    return;
                }
                if (_menu.Item("HealAlly").GetValue<bool>() &&
                    (hero.Health / hero.MaxHealth) * 100 <= _menu.Item("HealAllyHP").GetValue<Slider>().Value &&
                    Player.ManaPercentage() >= _menu.Item("minmanaE").GetValue<Slider>().Value && _w.IsReady() &&
                    hero.Distance(Player.ServerPosition) <= _w.Range)
                {
                    _w.Cast(hero);
                }
            }
        }

        #endregion

        #region Combo

        private static void Combo()
        {
            var target = TargetSelector.GetTarget(_w.Range, TargetSelector.DamageType.Magical);
            if (target == null || !target.IsValid)
            {
                return;
            }

            if (_menu.Item("QCombo").GetValue<bool>() && _q.IsReady())
            {
                _q.CastIfHitchanceEquals(target, HitChance.High);
            }

            if (_menu.Item("WCombo").GetValue<bool>() && _w.IsReady())
            {
                _w.Cast();
            }



            if (_menu.Item("ECombo").GetValue<bool>() && _e.IsReady())
            {
                //_e.Cast(Player);
                foreach (var ally in from ally in ObjectManager.Get<Obj_AI_Hero>()
                                       where (ally.IsAlly) 
                                       where _menu.Item("casteonally" + ally.BaseSkinName).GetValue<bool>()
                                       && (ObjectManager.Player.ServerPosition.Distance(ally.Position) < _e.Range)
                                     select ally)
                {
                    _e.Cast(ally);
                }
            }

            if (_menu.Item("RCombo").GetValue<bool>() && !_q.IsReady() && _r.IsReady() &&
                Player.CountEnemiesInRange(_w.Range) >= _menu.Item("RCount").GetValue<Slider>().Value &&
                _r.IsInRange(target.ServerPosition))
            {
                _r.CastIfHitchanceEquals(target, HitChance.High);
            }

            if (Player.Distance(target) <= 600 && IgniteDamage(target) >= target.Health &&
                _menu.Item("UseIgnite").GetValue<bool>())
            {
                Player.Spellbook.CastSpell(_ignite, target);
            }
        }

        #endregion

        #region Harass

        private static void Harass()
        {
            var target = TargetSelector.GetTarget(_w.Range, TargetSelector.DamageType.Magical);
            if (target == null || !target.IsValid)
            {
                return;
            }

            if (_menu.Item("HarassQ").GetValue<bool>() && _q.IsReady())
            {
                _q.CastIfHitchanceEquals(target, HitChance.Medium);
            }

            if (_menu.Item("HarassW").GetValue<bool>() && _w.IsReady())
            {
                _w.Cast(target);
            }

            if (_menu.Item("HarassE").GetValue<bool>() && _e.IsReady())
            {
                _e.Cast(Player);
            }
        }

        #endregion

        #region Ignite

        private static float IgniteDamage(Obj_AI_Hero target)
        {
            if (_ignite == SpellSlot.Unknown || Player.Spellbook.CanUseSpell(_ignite) != SpellState.Ready)
            {
                return 0f;
            }
            return (float)Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite);
        }

        #endregion

        #region Intterupt
        private static void Interrupter2_OnInterruptableTarget(Obj_AI_Hero sender,
        Interrupter2.InterruptableTargetEventArgs args)
        {
            if (args.DangerLevel != Interrupter2.DangerLevel.High || sender.Distance(ObjectManager.Player) > _q.Range)
            {
                return;
            }

            if (sender.IsValidTarget(_q.Range) && args.DangerLevel == Interrupter2.DangerLevel.High && _q.IsReady())
            {
                _q.Cast(ObjectManager.Player);
            }

            else if (sender.IsValidTarget(_q.Range) && args.DangerLevel == Interrupter2.DangerLevel.High && _q.IsReady() &&
                     !_q.IsReady())
            {
                _q.Cast(ObjectManager.Player);
            }
        }

        private static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (!gapcloser.Sender.IsValidTarget(_q.Range))
            {
                return;
            }

            if (gapcloser.Sender.Distance(ObjectManager.Player) > _q.Range)
            {
                return;
            }

            if (gapcloser.Sender.IsValidTarget(_q.Range))
            {
                if (_menu.Item("InterQ").GetValue<bool>() && _q.IsReady())
                {
                    _q.CastIfHitchanceEquals(gapcloser.Sender, HitChance.Medium);
                }
                    
                if (_menu.Item("InterR").GetValue<bool>() && !_q.IsReady() && _r.IsReady())
                {
                    _r.CastIfHitchanceEquals(gapcloser.Sender, HitChance.High);
                }
            }
        }

        #endregion

        #region Menu

        private static void InitializeMenu()
        {
            _menu = new Menu("ElNami", hero, true);

            //Orbwalker
            var orbwalkerMenu = new Menu("Orbwalker", "orbwalker");
            _orbwalker = new Orbwalking.Orbwalker(orbwalkerMenu);
            _menu.AddSubMenu(orbwalkerMenu);

            //TargetSelector
            var targetSelector = new Menu("Target Selector", "TargetSelector");
            TargetSelector.AddToMenu(targetSelector);
            _menu.AddSubMenu(targetSelector);

            //Combo
            var comboMenu = _menu.AddSubMenu(new Menu("Combo", "Combo"));
            comboMenu.AddItem(new MenuItem("QCombo", "Use Q").SetValue(true));
            comboMenu.AddItem(new MenuItem("WCombo", "Use W").SetValue(true));
            comboMenu.AddItem(new MenuItem("ECombo", "Use E").SetValue(true));
            comboMenu.AddItem(new MenuItem("RCombo", "Use R").SetValue(true));
            comboMenu.AddItem(new MenuItem("RCount", "Use ult in combo if enemies >= ")).SetValue(new Slider(2, 1, 5));
            comboMenu.AddItem(new MenuItem("UseIgnite", "Use Ignite in combo when killable").SetValue(true));
            comboMenu.AddItem(new MenuItem("ComboActive", "Combo!").SetValue(new KeyBind(32, KeyBindType.Press)));

            // e
            var castEMenu = _menu.AddSubMenu(new Menu("Cast E on", "CastEOn"));
            foreach (var ally in ObjectManager.Get<Obj_AI_Hero>().Where(champ => champ.IsAlly))
            {
                castEMenu.AddItem(
                    new MenuItem("casteonally" + ally.BaseSkinName, string.Format("Ult {0}", ally.BaseSkinName)).SetValue(true));
            }


            //Harass
            var harassMenu = _menu.AddSubMenu(new Menu("Harass", "H"));
            harassMenu.AddItem(new MenuItem("HarassQ", "Use Q").SetValue(true));
            harassMenu.AddItem(new MenuItem("HarassW", "Use W").SetValue(false));
            harassMenu.AddItem(new MenuItem("HarassE", "Use E").SetValue(true));
            harassMenu.AddItem(
                new MenuItem("HarassActive", "Harass!").SetValue(new KeyBind("C".ToCharArray()[0], KeyBindType.Press)));

            //Healing
            var healMenu = _menu.AddSubMenu(new Menu("Heal settings", "SH"));
            healMenu.AddItem(new MenuItem("SelfHeal", "Auto heal yourself").SetValue(true));
            healMenu.AddItem(new MenuItem("SelfHperc", "Self heal at >= ").SetValue(new Slider(25, 1, 100)));

            healMenu.AddItem(new MenuItem("HealAlly", "Auto heal ally's").SetValue(true));
            healMenu.AddItem(new MenuItem("HealAllyHP", "Heal ally at >= ").SetValue(new Slider(25, 1, 100)));
            healMenu.AddItem(new MenuItem("minmanaE", "Min % mana for heal")).SetValue(new Slider(55));

            //Interupt
            var interruptMenu = _menu.AddSubMenu(new Menu("Interrupt", "I"));
            interruptMenu.AddItem(new MenuItem("InterQ", "Use Q").SetValue(true));
            interruptMenu.AddItem(new MenuItem("InterR", "Use R").SetValue(false));

            //Here comes the moneyyy, money, money, moneyyyy
            var credits = _menu.AddSubMenu(new Menu("Credits", "jQuery"));
            credits.AddItem(new MenuItem("ElRengar.Paypal", "if you would like to donate via paypal:"));
            credits.AddItem(new MenuItem("ElRengar.Email", "info@zavox.nl"));

            _menu.AddItem(new MenuItem("422442fsaafs4242f", ""));
            _menu.AddItem(new MenuItem("422442fsaafsf", "Version: 1.0.0.2"));
            _menu.AddItem(new MenuItem("fsasfafsfsafsa", "Made By jQuery"));

            _menu.AddToMainMenu();
        }

        #endregion
    }
}