using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using Color = System.Drawing.Color;

namespace Mordekaiser
{
    class Program
    {
        public const string ChampionName = "Mordekaiser";
        public static readonly Obj_AI_Hero Player = ObjectManager.Player;

        public static Orbwalking.Orbwalker Orbwalker;

        public static List<Spell> SpellList = new List<Spell>();
        public static Spell Q, W, E, R;

        public static Items.Item Dfg = new Items.Item(3128, 700);
        public static Items.Item Bft = new Items.Item(3188, 700);

        public static Menu Config;
        public static Menu MenuExtras;
        public static float SlaveDelay = 0;

        public static float SlaveTimer;

        public static SpellSlot IgniteSlot = Player.GetSpellSlot("SummonerDot");

        private const float WDamageRange = 270f;
        private const float SlaveActivationRange = 2200f;

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
            CustomEvents.Unit.OnLevelUp += OnLevelUp;
        }

        private static void OnLevelUp(Obj_AI_Base sender, CustomEvents.Unit.OnLevelUpEventArgs args)
        {
            if (!sender.IsMe)
                return;

            ObjectManager.Player.Spellbook.LevelUpSpell(SpellSlot.R);
            ObjectManager.Player.Spellbook.LevelUpSpell(SpellSlot.E);
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            if (Player.BaseSkinName != ChampionName) return;
            if (Player.IsDead) return;

            SlaveTimer = Game.Time;

            /* [ Set Spells ]*/
            Q = new Spell(SpellSlot.Q, 300);
            SpellList.Add(Q);

            W = new Spell(SpellSlot.W, 780);
            W.SetTargetted(0.5f, 1500f);
            SpellList.Add(W);

            E = new Spell(SpellSlot.E, 665);
            E.SetSkillshot(0.25f, 15f * 2 * (float)Math.PI / 180, 2000f, false, SkillshotType.SkillshotCone);
            SpellList.Add(E);

            R = new Spell(SpellSlot.R, 850);
            R.SetTargetted(0.5f, 1500f);
            SpellList.Add(R);

            /* [ Set Menu ] */
            Config = new Menu(string.Format("+{0}+", ChampionName), ChampionName, true);
            Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));

            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            SimpleTs.AddToMenu(targetSelectorMenu);
            Config.AddSubMenu(targetSelectorMenu);

            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));

            /* [ Combo ] */
            Config.AddSubMenu(new Menu("Combo", "Combo"));
            Config.SubMenu("Combo").AddItem(new MenuItem("ComboUseQ", "Use Q").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("ComboUseW", "Use W").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("ComboUseE", "Use E").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("ComboUseR", "Use R").SetValue(true));

            Config.SubMenu("Combo").AddSubMenu(new Menu("Don't Use Ult On ->", "DontUlt"));
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.Team != Player.Team))
            {
                Config.SubMenu("Combo")
                    .SubMenu("DontUlt")
                    .AddItem(new MenuItem("DontUlt" + enemy.BaseSkinName, enemy.BaseSkinName).SetValue(false));
            }
            Config.SubMenu("Combo")
                .AddItem(
                    new MenuItem("ComboActive", "Combo!").SetValue(new KeyBind("Z".ToCharArray()[0], KeyBindType.Press)));

            /* [ Harass ] */
            Config.AddSubMenu(new Menu("Harass", "Harass"));
            Config.SubMenu("Harass").AddItem(new MenuItem("HarassUseQ", "Use Q").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("HarassUseW", "Use W").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("HarassUseE", "Use E").SetValue(true));
            Config.SubMenu("Harass")
                .AddItem(
                    new MenuItem("HarassActiveT", "Harass (Toggle)!").SetValue(new KeyBind("H".ToCharArray()[0],
                        KeyBindType.Toggle)));
            Config.SubMenu("Harass")
                .AddItem(
                    new MenuItem("HarassActive", "Harass!").SetValue(new KeyBind("C".ToCharArray()[0], KeyBindType.Press)));

            /* [ Farming ] */
            Config.AddSubMenu(new Menu("Lane Clear", "LaneClear"));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("LaneClearUseQ", "Use Q").SetValue(true));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("LaneClearUseW", "Use W").SetValue(true));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("LaneClearUseE", "Use E").SetValue(true));
            Config.SubMenu("LaneClear")
                .AddItem(new MenuItem("LaneClearActive", "Lane Clear!").SetValue(new KeyBind("V".ToCharArray()[0],
                        KeyBindType.Press)));

            /* [ JungleFarm ] */
            Config.AddSubMenu(new Menu("JungleFarm", "JungleFarm"));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("JungleFarmUseQ", "Use Q").SetValue(true));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("JungleFarmUseW", "Use W").SetValue(true));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("JungleFarmUseE", "Use E").SetValue(true));
            Config.SubMenu("JungleFarm")
                .AddItem(new MenuItem("JungleFarmActive", "Jungle Farm!").SetValue(new KeyBind("V".ToCharArray()[0],
                        KeyBindType.Press)));


            /* [ Drawing ] */
            Config.AddSubMenu(new Menu("Drawings", "Drawings"));
            Config.SubMenu("Drawings").AddItem(new MenuItem("DrawW", "W Available Range").SetValue(new Circle(true, Color.Pink)));
            Config.SubMenu("Drawings").AddItem(new MenuItem("DrawWAffectedRange", "W Affected Range").SetValue(new Circle(true, Color.Pink)));
            Config.SubMenu("Drawings").AddItem(new MenuItem("DrawE", "E Range").SetValue(new Circle(true, Color.Pink)));
            Config.SubMenu("Drawings").AddItem(new MenuItem("DrawR", "R Range").SetValue(new Circle(false, Color.Pink)));
            Config.SubMenu("Drawings").AddItem(new MenuItem("DrawEmpty", ""));
            Config.SubMenu("Drawings").AddItem(new MenuItem("DrawAloneEnemy", "Q Alone Target").SetValue(new Circle(true, Color.Pink)));

            Config.SubMenu("Drawings").AddItem(new MenuItem("DrawEmpty", ""));
            Config.SubMenu("Drawings").AddItem(new MenuItem("DrawDisable", "Disable All").SetValue(false));
            Config.SubMenu("Drawings").AddItem(new MenuItem("DrawEmpty", ""));

            Config.AddToMainMenu();

            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;

            WelcomeMessage();

        }

        private static bool MordekaiserHaveSlave
        {
            get { return Player.Spellbook.GetSpell(SpellSlot.R).Name == "mordekaisercotgguide"; }
        }

        private static void MordekaiserHaveSlave2()
        {
            if (Player.Spellbook.GetSpell(SpellSlot.R).Name == "mordekaisercotgguide")
            {
                if (SlaveTimer + 11000 < Game.Time)
                    SlaveTimer = Game.Time;
            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (MordekaiserHaveSlave)
            {
                MordekaiserHaveSlave2();
            }

            if (Config.Item("DrawDisable").GetValue<bool>())
                return;

            var drawThickness = 3;//Config.Item("DrawThickness" ).GetValue<Slider>().Value;
            var drawQuality = 15;//Config.Item("DrawQuality").GetValue<Slider>().Value;

            foreach (var spell in SpellList.Where(spell => spell != Q && spell != W))
            {
                var menuItem = Config.Item("Draw" + spell.Slot).GetValue<Circle>();
                if (menuItem.Active && spell.Level > 0)
                    Utility.DrawCircle(Player.Position, spell.Range, menuItem.Color, drawThickness, drawQuality);
            }
        }

        // Main game update function
        private static void Game_OnGameUpdate(EventArgs args)
        {
            // Nothing to do if the huekaiser is dead
            if (Player.IsDead) return;

            // if we cant move yet we cant?
            if (!Orbwalking.CanMove(100)) return;

            // Set movement and attacking via the orbwalker on in case of errors
            Orbwalker.SetAttack(true);
            Orbwalker.SetMovement(true);

            // -----------------------------------------------
            // ---- EMERGENCY MODE!!!!-----
            // -----------------------------------------------
            // Check to see if we should be in emergency mode 1
            if (ObjectManager.Player.Health * 100 / ObjectManager.Player.MaxHealth < 15)
            {
                var useW = Config.Item("ComboUseW").GetValue<bool>();
                var useR = Config.Item("ComboUseR").GetValue<bool>();
                var rTarget = SimpleTs.GetTarget(R.Range, SimpleTs.DamageType.Magical);

                // Use a red potion if we can
                if (Items.CanUseItem(2003)) { Items.UseItem(2003); }

                // Check if we can use our R to save us
                if (useR && rTarget != null && !MordekaiserHaveSlave)
                {
                    Orbwalker.SetMovement(false);
                    R.CastOnUnit(rTarget);
                    if (Items.CanUseItem(3090)) { Items.UseItem(3090); }
                    else if (Items.CanUseItem(3157)) { Items.UseItem(3157); }
                    Orbwalker.SetMovement(true);
                }
                // Save attempt without the R spell
                else
                {
                    Orbwalker.SetMovement(false);
                    if (Items.CanUseItem(3090)) { Items.UseItem(3090); }
                    else if (Items.CanUseItem(3157)) { Items.UseItem(3157); }
                    Orbwalker.SetMovement(true);
                }
                
            }
            // Check to see if we should be in emergency mode 2
            else if (ObjectManager.Player.Health * 100 / ObjectManager.Player.MaxHealth < 25)
            {
                // Check for a W target and usability
                var useW = Config.Item("ComboUseW").GetValue<bool>();
                var wTarget = SimpleTs.GetTarget(W.Range / 2, SimpleTs.DamageType.Magical);
                // Use W is we have an enemy player in metal shard range
                if (useW && wTarget != null && Player.Distance(wTarget) < WDamageRange)
                    W.CastOnUnit(Player);
                if (Items.CanUseItem(2003)) { Items.UseItem(2003); }
            }
            // -----------------------------------------------
            // -----------------------------------------------

            // Run the correct function for whichever button is held down
            if (Config.Item("ComboActive").GetValue<KeyBind>().Active)
                Combo();
            else if (Config.Item("HarassActive").GetValue<KeyBind>().Active || Config.Item("HarassActiveT").GetValue<KeyBind>().Active)
                Harass();
            else if (Config.Item("LaneClearActive").GetValue<KeyBind>().Active)
                LaneClear();
            else if (Config.Item("JungleFarmActive").GetValue<KeyBind>().Active)
                JungleFarm();

            // Auto Slave Control
            var rGhostArea = SimpleTs.GetTarget(2200f, SimpleTs.DamageType.Magical);
            if (MordekaiserHaveSlave && rGhostArea != null && Environment.TickCount >= SlaveDelay)
            {
                R.Cast(rGhostArea);
                SlaveDelay = Environment.TickCount + 1000;
            }

        }



        // Main combo logic function
        private static void Combo()
        {
            // initalize variables
            var wTarget = SimpleTs.GetTarget(W.Range / 2, SimpleTs.DamageType.Magical);
            var eTarget = SimpleTs.GetTarget(E.Range, SimpleTs.DamageType.Magical);
            var rTarget = SimpleTs.GetTarget(R.Range, SimpleTs.DamageType.Magical);
            var useQ = Config.Item("ComboUseQ").GetValue<bool>();
            var useW = Config.Item("ComboUseW").GetValue<bool>();
            var useE = Config.Item("ComboUseE").GetValue<bool>();
            var useR = Config.Item("ComboUseR").GetValue<bool>();

            // Filter out people we shouldnt ult
            useR = (Config.Item("DontUlt" + rTarget.BaseSkinName) != null &&
                    Config.Item("DontUlt" + rTarget.BaseSkinName).GetValue<bool>() == false) && useR;

            // Shut off auto attack via orbwalker so it doesnt interrupt any combos
            Orbwalker.SetAttack(false);


            // -----------------------------------------------
            // ----  MAIN ATTACK LOGIC START
            // -----------------------------------------------
            // ----------------------------------------------------------------------------------------
            // First priority is to see if there is anyone in E range that can be insta killed
            if (useE && eTarget != null && eTarget.Health < Player.GetSpellDamage(eTarget, SpellSlot.E))
                E.Cast(eTarget.Position);
            // See if DFG or BFT are useable
            if (Items.CanUseItem(3128) || Items.CanUseItem(3188))
            {
                // See if we had a DFG
                if (useE && eTarget != null && Items.CanUseItem(3128))
                {
                    if (eTarget.Health < Player.GetSpellDamage(eTarget, SpellSlot.E) + Player.GetItemDamage(eTarget, Damage.DamageItems.Dfg) + (Player.GetSpellDamage(eTarget, SpellSlot.E) * 0.15))
                    {
                        Items.UseItem(3128, eTarget);
                        E.Cast(eTarget.Position);
                    }
                }
                // Or else we had a BFT
                else if (useE && eTarget != null && Items.CanUseItem(3188))
                {
                    if (eTarget.Health < Player.GetSpellDamage(eTarget, SpellSlot.E) + Player.GetItemDamage(eTarget, Damage.DamageItems.BlackFireTorch) + (Player.GetSpellDamage(eTarget, SpellSlot.E) * 0.15))
                    {
                        Items.UseItem(3188, eTarget);
                        E.Cast(eTarget.Position);
                    }
                }
            }
            // ----------------------------------------------------------------------------------------

            // ----------------------------------------------------------------------------------------
            // Second priority is to see if there is anyone in R range that can be insta killed without any of the DOT
            if (useR && rTarget != null && !MordekaiserHaveSlave && rTarget.Health < (Player.GetSpellDamage(rTarget, SpellSlot.R) / 2))
                R.CastOnUnit(rTarget);
            // See if DFG or BFT are useable
            if (Items.CanUseItem(3128) || Items.CanUseItem(3188))
            {
                // See if we had a DFG
                if (Items.CanUseItem(3128))
                {
                    if (useR && rTarget != null && !MordekaiserHaveSlave && Player.Distance(rTarget) < 695)
                    {
                        if (rTarget.Health < (Player.GetSpellDamage(rTarget, SpellSlot.R) / 2) + Player.GetItemDamage(rTarget, Damage.DamageItems.Dfg) + ((Player.GetSpellDamage(rTarget, SpellSlot.R) / 2) * 0.15))
                        {
                            if (Player.Distance(rTarget) < 599 && IgniteSlot != SpellSlot.Unknown && Player.SummonerSpellbook.CanUseSpell(IgniteSlot) == SpellState.Ready)
                                Player.SummonerSpellbook.CastSpell(IgniteSlot, rTarget);
                            Items.UseItem(3128, rTarget);
                            R.CastOnUnit(rTarget);
                        }

                    }
                }
                // Or else we had a BFT
                else if (Items.CanUseItem(3188))
                {
                    if (useR && rTarget != null && !MordekaiserHaveSlave && Player.Distance(rTarget) < 695)
                    {
                        if (rTarget.Health < (Player.GetSpellDamage(rTarget, SpellSlot.R) / 2) + Player.GetItemDamage(rTarget, Damage.DamageItems.BlackFireTorch) + ((Player.GetSpellDamage(rTarget, SpellSlot.R) / 2) * 0.15))
                        {
                            if (Player.Distance(rTarget) < 599 && IgniteSlot != SpellSlot.Unknown && Player.SummonerSpellbook.CanUseSpell(IgniteSlot) == SpellState.Ready)
                                Player.SummonerSpellbook.CastSpell(IgniteSlot, rTarget);
                            Items.UseItem(3188, rTarget);
                            R.CastOnUnit(rTarget);
                        }

                    }
                }
            }
            // ----------------------------------------------------------------------------------------

            // ----------------------------------------------------------------------------------------
            // Thrid priority is to see if there is anyone in E range that can be insta killed without any of the DOT using both E and R
            if (useR && useE && eTarget != null && !MordekaiserHaveSlave && eTarget.Health < Player.GetSpellDamage(eTarget, SpellSlot.E) + (Player.GetSpellDamage(eTarget, SpellSlot.R) / 2))
            {
                if (Player.Distance(eTarget) < 599 && IgniteSlot != SpellSlot.Unknown && Player.SummonerSpellbook.CanUseSpell(IgniteSlot) == SpellState.Ready)
                    Player.SummonerSpellbook.CastSpell(IgniteSlot, eTarget);
                E.Cast(eTarget.Position);
                R.CastOnUnit(eTarget);
            }
            // See if DFG or BFT are useable
            if (Items.CanUseItem(3128) || Items.CanUseItem(3188))
            {
                // See if we had a DFG
                if (useE && useR && eTarget != null && Items.CanUseItem(3128))
                {
                    if (eTarget.Health < Player.GetSpellDamage(eTarget, SpellSlot.E) + Player.GetItemDamage(eTarget, Damage.DamageItems.Dfg) + (Player.GetSpellDamage(eTarget, SpellSlot.E) * 0.15) + (Player.GetSpellDamage(rTarget, SpellSlot.R) / 2) + ((Player.GetSpellDamage(rTarget, SpellSlot.R) / 2) * 0.15))
                    {
                        if (Player.Distance(eTarget) < 599 && IgniteSlot != SpellSlot.Unknown && Player.SummonerSpellbook.CanUseSpell(IgniteSlot) == SpellState.Ready)
                            Player.SummonerSpellbook.CastSpell(IgniteSlot, eTarget);
                        Items.UseItem(3128, eTarget);
                        E.Cast(eTarget.Position);
                        R.CastOnUnit(eTarget);
                    }
                }
                // Or else we had a BFT
                else if (useE && useR && eTarget != null && Items.CanUseItem(3188))
                {
                    if (eTarget.Health < Player.GetSpellDamage(eTarget, SpellSlot.E) + Player.GetItemDamage(eTarget, Damage.DamageItems.BlackFireTorch) + (Player.GetSpellDamage(eTarget, SpellSlot.E) * 0.15) + (Player.GetSpellDamage(rTarget, SpellSlot.R) / 2) + ((Player.GetSpellDamage(rTarget, SpellSlot.R) / 2) * 0.15))
                    {
                        if (Player.Distance(eTarget) < 599 && IgniteSlot != SpellSlot.Unknown && Player.SummonerSpellbook.CanUseSpell(IgniteSlot) == SpellState.Ready)
                            Player.SummonerSpellbook.CastSpell(IgniteSlot, eTarget);
                        Items.UseItem(3188, eTarget);
                        E.Cast(eTarget.Position);
                        R.CastOnUnit(eTarget);
                    }
                }
            }
            // ----------------------------------------------------------------------------------------

            // ----------------------------------------------------------------------------------------
            // Next priority is to see if Q is up and if we have a target in autoattack range
            if (useQ && Q.IsReady() && Player.Distance(wTarget) < Orbwalking.GetRealAutoAttackRange(ObjectManager.Player))
            {
                // Shut off movement via orbwalker so it doesnt interrupt any combos
                Orbwalker.SetMovement(false);
                // See if we can Q + E them for a kill using at least 1/3 of Q damage
                if (useE && E.IsReady() && wTarget.Health < (Player.GetSpellDamage(wTarget, SpellSlot.Q) / 3) + Player.GetSpellDamage(wTarget, SpellSlot.E))
                {
                    Q.Cast();
                    Player.IssueOrder(GameObjectOrder.AttackUnit, wTarget);
                    E.Cast(wTarget.Position);
                }
                // See if we can Q + E + R them for a kill using at least 1/3 of Q damage
                else if (useE && E.IsReady() && useR && R.IsReady() && !MordekaiserHaveSlave && wTarget != null && wTarget.Health < (Player.GetSpellDamage(wTarget, SpellSlot.Q) / 3) + Player.GetSpellDamage(wTarget, SpellSlot.E) + (Player.GetSpellDamage(wTarget, SpellSlot.R) / 2))
                {
                    Q.Cast();
                    Player.IssueOrder(GameObjectOrder.AttackUnit, wTarget);
                    E.Cast(wTarget.Position);
                    R.CastOnUnit(wTarget);

                }
                // Couldn't combo anyone so we just use Q for dps
                else if (wTarget != null)
                {
                    Q.Cast();
                    Player.IssueOrder(GameObjectOrder.AttackUnit, wTarget);
                }
                // Turn movement via orbwalker since we are done with combos
                Orbwalker.SetMovement(true);
            }
            // ----------------------------------------------------------------------------------------

            // -----------------------------------------------
            // Couldn't combo anyone so we just use E for dps
            if (useE && eTarget != null)
                E.Cast(eTarget.Position);

            // Use W is we have an enemy player in metal shard range
            if (useW && wTarget != null && Player.Distance(wTarget) < WDamageRange)
                W.CastOnUnit(Player);

            // Turn on autoattack via orbwalker since we are done casting spells
            Orbwalker.SetAttack(true);
            
            
            // ----------------------------------------------------------------------------------------
            // -----------------------------------------------
            // ----  MAIN ATTACK LOGIC END
            // -----------------------------------------------
        }

        private static void Harass()
        {
            var wTarget = SimpleTs.GetTarget(W.Range, SimpleTs.DamageType.Magical);
            var eTarget = SimpleTs.GetTarget(E.Range, SimpleTs.DamageType.Magical);

            var useQ = Config.Item("ComboUseQ").GetValue<bool>();
            var useW = Config.Item("ComboUseW").GetValue<bool>();
            var useE = Config.Item("ComboUseE").GetValue<bool>();

            if (useQ && Q.IsReady() && Player.Distance(wTarget) <= Orbwalking.GetRealAutoAttackRange(ObjectManager.Player))
                Q.Cast();

            if (useW && wTarget != null && Player.Distance(wTarget) <= WDamageRange)
                W.CastOnUnit(Player);

            if (useE && eTarget != null)
                E.Cast(eTarget.Position);
        }





        private static void LaneClear()
        {
            var useQ = Config.Item("LaneClearUseQ").GetValue<bool>();
            var useW = Config.Item("LaneClearUseW").GetValue<bool>();
            var useE = Config.Item("LaneClearUseE").GetValue<bool>();

            if (useQ && Q.IsReady())
            {
                var minionsQ = MinionManager.GetMinions(Player.ServerPosition,
                    Orbwalking.GetRealAutoAttackRange(ObjectManager.Player), MinionTypes.All, MinionTeam.NotAlly);
                foreach (var vMinion in from vMinion in minionsQ
                                        let vMinionEDamage = Player.GetSpellDamage(vMinion, SpellSlot.Q)
                                        //where vMinion.Health <= vMinionEDamage && vMinion.Health > Player.GetAutoAttackDamage(vMinion)
                                        select vMinion)
                {
                    Q.Cast(vMinion);
                }
            }

            if (useW && W.IsReady())
            {
                var rangedMinionsW = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, W.Range);
                var minionsW = W.GetCircularFarmLocation(rangedMinionsW, W.Range * 0.3f);
                if (minionsW.MinionsHit <= 4 || !W.InRange(minionsW.Position.To3D()))
                    return;
                W.CastOnUnit(Player);
            }

            if (useE && E.IsReady())
            {
                var rangedMinionsE = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, E.Range);
                var minionsE = E.GetCircularFarmLocation(rangedMinionsE, E.Range);
                if (minionsE.MinionsHit <= 3 || !E.InRange(minionsE.Position.To3D()))
                    return;
                E.Cast(minionsE.Position);
            }
        }


        private static void JungleFarm()
        {
            var useQ = Config.Item("JungleFarmUseQ").GetValue<bool>();
            var useW = Config.Item("JungleFarmUseW").GetValue<bool>();
            var useE = Config.Item("JungleFarmUseE").GetValue<bool>();

            var mobs = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, W.Range / 2, MinionTypes.All,
                MinionTeam.Neutral, MinionOrderTypes.MaxHealth);

            if (mobs.Count <= 0) return;
            var mob = mobs[0];

            if (useQ && Q.IsReady())
                Q.Cast();

            if (useW && W.IsReady())
                W.CastOnUnit(Player);

            if (useE && E.IsReady())
                E.Cast(mob.Position);
        }

        private static bool TargetAlone(Obj_AI_Hero vTarget)
        {
            var objects =
                ObjectManager.Get<Obj_AI_Base>()
                    .Where(x => x.IsMinion && x.IsEnemy && x.IsValid && vTarget.Distance(x) < 240);
            return !objects.Any();
        }

        private static double CalcQDamage
        {
            get
            {
                var qDamageVisitors = new float[] { 80, 110, 140, 170, 200 };
                var qDamageAlone = new float[] { 132, 181, 230, 280, 330 };

                var qTarget = SimpleTs.GetTarget(600, SimpleTs.DamageType.Magical);

                var fxQDamage = TargetAlone(qTarget)
                    ? qDamageAlone[Q.Level] + Player.BaseAttackDamage * 1.65 + Player.BaseAbilityDamage * .66
                    : qDamageVisitors[Q.Level] + Player.BaseAttackDamage + Player.BaseAbilityDamage * .40;
                return fxQDamage;
            }
        }

        private static float GetComboDamage(Obj_AI_Hero vTarget)
        {
            var fComboDamage = 0d;

            if (Q.IsReady() && Player.Distance(vTarget) < Orbwalking.GetRealAutoAttackRange(Player))
                fComboDamage += Player.GetSpellDamage(vTarget, SpellSlot.Q);

            if (W.IsReady() && Player.Distance(vTarget) < WDamageRange)
                fComboDamage += Player.GetSpellDamage(vTarget, SpellSlot.W);

            if (E.IsReady() && Player.Distance(vTarget) < E.Range)
                fComboDamage += Player.GetSpellDamage(vTarget, SpellSlot.E);

            if (R.IsReady() && Player.Distance(vTarget) < R.Range)
                fComboDamage += Player.GetSpellDamage(vTarget, SpellSlot.R);

            if (IgniteSlot != SpellSlot.Unknown && Player.SummonerSpellbook.CanUseSpell(IgniteSlot) == SpellState.Ready &&
                Player.Distance(vTarget) < R.Range)
                fComboDamage += Player.GetSummonerSpellDamage(vTarget, Damage.SummonerSpell.Ignite);

            if (Items.CanUseItem(3128) && Player.Distance(vTarget) < R.Range)
                fComboDamage += Player.GetItemDamage(vTarget, Damage.DamageItems.Dfg);

            if (Items.CanUseItem(3092) && Player.Distance(vTarget) < R.Range)
                fComboDamage += Player.GetItemDamage(vTarget, Damage.DamageItems.FrostQueenClaim);

            return (float)fComboDamage;
        }

        private static void WelcomeMessage()
        {
            Game.PrintChat(String.Format("-----------------------------------"));
            Game.PrintChat(String.Format("Faiolas Custom Mordekaiser Loaded!!"));
            Game.PrintChat(String.Format("-----------------------------------"));
        }
    }
}
