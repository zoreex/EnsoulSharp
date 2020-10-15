using SharpDX;

namespace mAxIO
{
    using System;
    using System.Linq;
    using EnsoulSharp;
    using EnsoulSharp.SDK;
    using EnsoulSharp.SDK.MenuUI;
    using EnsoulSharp.SDK.MenuUI.Values;
    using EnsoulSharp.SDK.Prediction;
    using EnsoulSharp.SDK.Utility;
    using Color = System.Drawing.Color;

    public static class maxioAshe
    {
        private static Menu MainMenu;

        private static Spell Q, W, R;

        public static void OnGameLoad()
        {
            // Declaring Spells & Info
            Q = new Spell(SpellSlot.Q, 575f);

            W = new Spell(SpellSlot.W, 1175);
            W.SetSkillshot(0.25f, 20f, 2000, true, SkillshotType.Cone);

            R = new Spell(SpellSlot.R, 1750);
            R.SetSkillshot(0.25f, 86f, 1600, true, SkillshotType.Line);

            // Main Menu
            MainMenu = new Menu("Budget Ashe", "Budget Ashe", true);

            // Combo Menu
            var comboMenu = new Menu("Combo", "Budget Combo")
            {
                new MenuSeparator("spacerCombo", "Budget Combo Menu"),
                new MenuBool("comboQ", "Use Q", true),
                new MenuBool("comboW", "Use W", true),
                new MenuBool("comboWonlyOutOfAA", "^ Use W only out of AA-Range", true),
                new MenuBool("comboR", "Use R", true),
                //new MenuSeparator("comboRclose", "^ Will aim Ult close to Cursor if out of AA Range")
                //new MenuSlider("comboRrange", "R Range", 0, 50, 6000)
            };
            MainMenu.Add(comboMenu);

            // Jungle Clear Menu
            var jungleMenu = new Menu("Jungle", "Budget Jungle")
            {
                new MenuSeparator("spacerJungle", "Budget Jungle Menu"),
                new MenuBool("jungleQ", "Use Q in Jungle", true),
            };
            MainMenu.Add(jungleMenu);

            // Auto Menu
            var autoMenu = new Menu("Autocast", "Budget Autocast")
            {
                new MenuSeparator("spacerAuto", "Budget Auto-Cast Menu"),
                new MenuBool("autoW", "Auto-Cast W on Stunned", true),
                new MenuBool("autoR", "Auto-Cast R on Stunned", false)
            };
            MainMenu.Add(autoMenu);

            // KillSteal Menu

            var killStealMenu = new Menu("Killsteal", "Budget Killsteal")
            {
                new MenuSeparator("spacerKillsteal", "Budget Killsteal Menu"),
                new MenuBool("killstealAA", "Killsteal with AA", true),
                new MenuSlider("killstealAALvl", "^ Min. Lvl for AA to Killsteal", 9, 1, 18),
                new MenuBool("killstealW", "Killsteal W", true)
            };
            MainMenu.Add(killStealMenu);


            // Draw Menu
            var drawMenu = new Menu("Drawing", "Budget Drawings")
            {
                new MenuSeparator("spacerDraw", "Budget Draw Menu"),
                new MenuBool("drawW", "W Range", true),
                new MenuBool("drawR", "R Range", true)
            };
            MainMenu.Add(drawMenu);

            MainMenu.Attach();

            Game.OnUpdate += OnUpdate;
            Orbwalker.OnAction += OrbwalkerModeCombo;

            Drawing.OnDraw += OnDraw;
        }

        private static void OrbwalkerModeCombo(object obj, OrbwalkerActionArgs args)
        {
            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                QCombo(args);

                WCombo(args);

                RCombo(args);
            }
        }

        private static void QCombo(OrbwalkerActionArgs args)
        {
            if (MainMenu["Combo"]["comboQ"].GetValue<MenuBool>().Enabled && Q.IsReady())
            {
                if (args.Type == OrbwalkerType.AfterAttack)
                {
                    Q.Cast();
                }
            }
        }

        private static void WCombo(OrbwalkerActionArgs args)
        {
            if (MainMenu["Combo"]["comboW"].GetValue<MenuBool>().Enabled && W.IsReady())
            {
                var target = TargetSelector.GetTarget(W.Range);
                var inAaRange = target.IsValidTarget(ObjectManager.Player.GetRealAutoAttackRange(target));
                var wPred = W.GetPrediction(target);
                var lethalOn =
                    ObjectManager.Player.HasBuff("ASSETS/Perks/Styles/Precision/LethalTempo/LethalTempoEmpowered.lua");

                if (!MainMenu["Combo"]["comboWonlyOutOfAA"].GetValue<MenuBool>().Enabled && inAaRange && !lethalOn)
                {
                    if (args.Type == OrbwalkerType.AfterAttack)
                    {
                        W.Cast(wPred.UnitPosition);
                    }
                }
                else if (target.IsValidTarget(W.Range) && !inAaRange && wPred.Hitchance >= HitChance.High)
                {
                    W.Cast(wPred.UnitPosition);
                }
            }
        }

        private static void RCombo(OrbwalkerActionArgs args)
        {
            if (MainMenu["Combo"]["comboR"].GetValue<MenuBool>().Enabled && R.IsReady())
            {
                AIHeroClient target;
                // select target
                if ((target = TargetSelector.GetTarget(ObjectManager.Player.GetRealAutoAttackRange())) == null)
                {
                    if ((target = TargetSelector.GetTargets(R.Range).OrderBy(x => x.DistanceToPlayer()).FirstOrDefault()) == null)
                        return;
                }
                else if (args.Type == OrbwalkerType.AfterAttack)
                    return;

                // attack target
                R.Cast(R.GetPrediction(target).CastPosition);
            }
        }

//        private static void AutoCastW2()
//        {
//            if (MainMenu["Autocast"]["autoW"].GetValue<MenuBool>().Enabled && W.IsReady())
//            {
//                AIHeroClient target;
//                if ((target = TargetSelector.GetTarget((W.Range))) == null)
//                {
//                    if ((target = TargetSelector.GetTargets(W.Range).Select(x => x.DistanceToPlayer()).FirstOrDefault()) == null)
//                    {
//                        W.Cast();
//                    }
//                }
//            }
//        }

        private static void AutoCastW()
        {
            if (MainMenu["Autocast"]["autoW"].GetValue<MenuBool>().Enabled && W.IsReady())
            {
                var target = TargetSelector.GetTarget(W.Range);
                var wPred = W.GetPrediction(target);
                if (target.IsValidTarget(W.Range) && wPred.Hitchance == HitChance.Immobile)
                {
                    W.Cast(wPred.UnitPosition);
                }
            }
        }

        private static void AutoCastR()
        {
            if (MainMenu["Autocast"]["autoR"].GetValue<MenuBool>().Enabled && R.IsReady())
            {
                var target = TargetSelector.GetTarget(R.Range);
                var rPred = R.GetPrediction(target);
                if (target.IsValidTarget(R.Range) && rPred.Hitchance == HitChance.Immobile)
                {
                    R.Cast(rPred.CastPosition);
                }
            }
        }

        private static void KillSteal()
        {
            if (MainMenu["Killsteal"]["killstealW"].GetValue<MenuBool>().Enabled && W.IsReady())
            {
                foreach (var target in GameObjects.EnemyHeroes.Where(enemy =>
                    enemy.IsValidTarget(W.Range - 65f) && !enemy.IsInvulnerable))
                {
                    var wPred = W.GetPrediction(target);
                    if (target.IsValid && target.Health < W.GetDamage(target) - 15f &&
                        wPred.Hitchance >= HitChance.Medium)
                    {
                        W.Cast(wPred.UnitPosition);
                    }
                }
            }

            if (ObjectManager.Player.Level <
                MainMenu["Killsteal"]["killstealAALvl"].GetValue<MenuSlider>().Value) return;
            {
                if (!MainMenu["Killsteal"]["killstealAA"].GetValue<MenuBool>().Enabled ||
                    !Orbwalker.CanAttack()) return;
                foreach (var target in GameObjects.EnemyHeroes.Where(enemy =>
                    enemy.IsValidTarget(600f) && !enemy.IsInvulnerable))
                {
                    if (target.IsValid && target.Health < ObjectManager.Player.GetAutoAttackDamage(target) - 30.00 &&
                        Orbwalker.ActiveMode == OrbwalkerMode.Combo)
                    {
                        Orbwalker.Attack(target);
                    }
                }
            }
        }

        // Combo Switch-Case
        private static void OnUpdate(EventArgs args)
        {
            if (ObjectManager.Player.IsDead || MenuGUI.IsChatOpen || ObjectManager.Player.IsRecalling()) return;
            KillSteal();
            AutoCastW();
            AutoCastR();
        }

        private static void OnDraw(EventArgs args)
        {
            if (MainMenu["Drawing"]["drawW"].GetValue<MenuBool>().Enabled)
            {
                Render.Circle.DrawCircle(ObjectManager.Player.Position, W.Range, Color.Aqua);
            }

            if (MainMenu["Drawing"]["drawR"].GetValue<MenuBool>().Enabled)
            {
                Render.Circle.DrawCircle(ObjectManager.Player.Position, R.Range, Color.IndianRed);
            }
        }
    }
}
