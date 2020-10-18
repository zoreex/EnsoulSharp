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
            W.SetSkillshot(0.25f, 20f, 2000, true, SkillshotType.Line);

            R = new Spell(SpellSlot.R, 1750);
            R.SetSkillshot(0.25f, 86f, 1600, true, SkillshotType.Line);

            // Main Menu
            MainMenu = new Menu("mAxIO Ashe", "mAxIO Ashe", true);

            // Combo Menu
            var comboMenu = new Menu("Combo", "Combo")
            {
                new MenuSeparator("spacerCombo", "Combo Menu"),
                new MenuBool("comboQ", "Use Q", true),
                new MenuBool("comboW", "Use W", true),
                new MenuBool("comboWonlyOutOfAA", "^ Use W only out of AA-Range", false),
                new MenuBool("comboR", "Use R", true),
            };
            MainMenu.Add(comboMenu);

            // Jungle Clear Menu
            var jungleMenu = new Menu("Jungle", "Jungle")
            {
                new MenuSeparator("spacerJungle", "Jungle Menu"),
                new MenuBool("jungleQ", "Use Q in Jungle", false),
                new MenuSlider("jungleQMana", "^ Min. Mana% for Q", 50, 0, 100),
                new MenuBool("jungleW", "Use W in Jungle", true),
                new MenuSlider("jungleWMana", "^ Min. Mana% for W", 30, 0, 100)
            };
            MainMenu.Add(jungleMenu);

            // Misc Menu - Auto - Killsteal - Drawing
            var miscMenu = new Menu("Misc", "Misc")
            {
                // AutoCast Menu
                new Menu("Autocast", "Autocast")
                {
                    new MenuSeparator("spacerAuto", "Auto-Cast Menu"),
                    new MenuBool("autoW", "Auto-Cast W on Stunned", true),
                    new MenuBool("autoR", "Auto-Cast R on Stunned", false)
                },
                // KillSteal Menu
                new Menu("Killsteal", "Killsteal")
                {
                    new MenuSeparator("spacerKillsteal", "Killsteal Menu"),
                    new MenuBool("killstealAA", "Killsteal with AutoAttack", true),
                    new MenuSlider("killstealAALvl", "^ Min. Lvl (9 Recommended)", 9, 1, 18),
                    new MenuBool("killstealW", "Killsteal W", true)
                },
                // Draw Menu
                new Menu("Drawing", "Drawings")
                {
                    new MenuSeparator("spacerDraw", "Drawings Menu"),
                    new MenuBool("drawW", "W Range", false),
                    new MenuBool("drawR", "R Range", true)
                }
            };
            MainMenu.Add(miscMenu);

            MainMenu.Attach();

            Game.OnUpdate += OnUpdate;
            Orbwalker.OnAction += OrbwalkerMode;

            Drawing.OnDraw += OnDraw;
        }

        private static void OrbwalkerMode(object obj, OrbwalkerActionArgs args)
        {
            if (Orbwalker.ActiveMode == EnsoulSharp.SDK.OrbwalkerMode.Combo)
            {
                QCombo(args);
                WCombo(args);
                RCombo(args);
            }

            if (Orbwalker.ActiveMode == EnsoulSharp.SDK.OrbwalkerMode.LaneClear)
            {
                LaneClear(args);
                JungleClear(args);
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
                if ((target = TargetSelector.GetTarget(ObjectManager.Player.GetRealAutoAttackRange())) == null)
                {
                    if ((target = TargetSelector.GetTargets(R.Range).OrderBy(x => x.DistanceToPlayer()).FirstOrDefault()) != null
                        && !target.IsUnderEnemyTurret())
                    {
                        R.Cast(R.GetPrediction(target).CastPosition);
                    }
                }
                else if (args.Type == OrbwalkerType.AfterAttack)
                {
                    R.Cast(R.GetPrediction(target).UnitPosition);
                }
            }
        }

        private static void LaneClear(OrbwalkerActionArgs args)
        {
            if (Orbwalker.LastTarget is AITurretClient && (args.Type == OrbwalkerType.AfterAttack) && Q.IsReady())
            {
                Q.Cast();
            }
        }

        // Rewrite this garbage shit it looks so fucking ugly but i dont have the iq atm
        private static void JungleClear(OrbwalkerActionArgs args)
        {
            if (MainMenu["Jungle"]["jungleQ"].GetValue<MenuBool>().Enabled
                && ObjectManager.Player.ManaPercent >= MainMenu["Jungle"]["jungleQMana"].GetValue<MenuSlider>())
            {
                if (args.Target != null && args.Target.Type == GameObjectType.AIMinionClient)
                {
                    var mob = GameObjects.Jungle.Where(x => x.IsValidTarget(Q.Range) && x.GetJungleType() != JungleType.Unknown)
                        .OrderByDescending(x => x.MaxHealth).FirstOrDefault();
                    if (mob != null && mob.IsValidTarget() && args.Type == OrbwalkerType.AfterAttack)
                    {
                        Q.Cast();
                    }
                }
            }

            if (MainMenu["Jungle"]["jungleW"].GetValue<MenuBool>().Enabled
                && ObjectManager.Player.ManaPercent >= MainMenu["Jungle"]["jungleWMana"].GetValue<MenuSlider>())
            {
                var mob = GameObjects.Jungle.Where(x => x.IsValidTarget(W.Range) && (x.GetJungleType() != JungleType.Unknown))
                    .OrderBy(x => x.DistanceToPlayer()).FirstOrDefault();
                if (mob != null && mob.IsValidTarget(W.Range) && mob.DistanceToPlayer() > ObjectManager.Player.GetRealAutoAttackRange() + 50f)
                {
                    var wMobPred = W.GetPrediction(mob);
                    W.Cast(wMobPred.UnitPosition);
                }
            }
        }

        private static void AutoCasting()
        {
            if (MainMenu["Misc"]["Autocast"]["autoW"].GetValue<MenuBool>().Enabled && W.IsReady())
            {
                var target = TargetSelector.GetTarget(W.Range);
                var wPred = W.GetPrediction(target);
                if (target.IsValidTarget(W.Range) && (target.IsStunned || target.IsAsleep || target.IsSuppressed || target.IsCharmed
                                                      || target.IsFleeing || target.IsFeared || target.IsTaunted))
                {
                    W.Cast(wPred.UnitPosition);
                }
            }

            if (MainMenu["Misc"]["Autocast"]["autoR"].GetValue<MenuBool>().Enabled && R.IsReady())
            {
                var target = TargetSelector.GetTarget(R.Range);
                var rPred = R.GetPrediction(target);
                if (target.IsValidTarget(R.Range) && (target.IsStunned || target.IsSuppressed || target.IsAsleep))
                {
                    R.Cast(rPred.CastPosition);
                }
            }
        }

        private static void KillSteal()
        {
            if (ObjectManager.Player.Level < MainMenu["Misc"]["Killsteal"]["killstealAALvl"].GetValue<MenuSlider>().Value)
                return;
            {
                if (MainMenu["Misc"]["Killsteal"]["killstealAA"].GetValue<MenuBool>().Enabled && Orbwalker.CanAttack())
                {
                    foreach (var target in GameObjects.EnemyHeroes.Where(enemy =>
                        enemy.IsValidTarget(585f) && !enemy.IsInvulnerable))
                    {
                        if (target.IsValid && target.Health < ObjectManager.Player.GetAutoAttackDamage(target) - 30.00 &&
                            Orbwalker.ActiveMode == EnsoulSharp.SDK.OrbwalkerMode.Combo)
                        {
                            Orbwalker.Attack(target);
                        }
                    }
                }
            }

            if (!MainMenu["Misc"]["Killsteal"]["killstealW"].GetValue<MenuBool>().Enabled || !W.IsReady())
                return;
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
        }

        private static void OnUpdate(EventArgs args)
        {
            if (ObjectManager.Player.IsDead || MenuGUI.IsChatOpen || ObjectManager.Player.IsRecalling()) return;
            KillSteal();
            AutoCasting();
        }

        private static void OnDraw(EventArgs args)
        {
            if (MainMenu["Misc"]["Drawing"]["drawW"].GetValue<MenuBool>().Enabled)
            {
                Render.Circle.DrawCircle(ObjectManager.Player.Position, W.Range, Color.Aqua);
            }

            if (MainMenu["Misc"]["Drawing"]["drawR"].GetValue<MenuBool>().Enabled)
            {
                Render.Circle.DrawCircle(ObjectManager.Player.Position, R.Range, Color.IndianRed);
            }
        }
    }
}