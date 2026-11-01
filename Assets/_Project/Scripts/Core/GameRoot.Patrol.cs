using System.Collections.Generic;
using Hollowbrook.Audio;
using Hollowbrook.Bootstrap;
using Hollowbrook.Checkpoint;
using Hollowbrook.Horror;
using Hollowbrook.Logic;
using Hollowbrook.UI;
using Hollowbrook.World;
using UnityEngine;

namespace Hollowbrook.Core
{
    // GameRoot, part: leaving the desk for the patrol, peeking into classrooms, coming back,
    // and opening the map.
    public partial class GameRoot
    {
        // ---- exploration ----

        public void EnterExploration(RoomId room = RoomId.Hallway)
        {
            if (HuntReady)
            {
                Session.Hunt = HuntState.Active;   // the Patrol click is what starts night 5's hunt
                PersistRun();
            }
            QueueSceneAction(() =>
            {
                SetState(AppViewState.Exploration);
                ObjectiveText = PatrolObjective();
                Bus.Publish(GameEvent.Of(EventIds.ObjectiveChanged, ObjectiveText));
                if (Views != null) Views.EnterRoom(room);
                Audio.PlayMusic(MusicForRoom(room));
                if (HuntActive && Views != null) Views.StartAdminHunt(BuildHuntTuning(), Session.Seed ^ 0x4A0B);
            });
            Hollowbrook.Bootstrap.SceneRouter.Load(Hollowbrook.Bootstrap.SceneRouter.School);
        }

        /// <summary>Is there a standing anomaly in this peek room right now? Which rooms have one is
        /// decided by PatrolCycle.StandingIn; it stays until the lamp drives it out. Night 5 has
        /// none: he is the only thing in the building.</summary>
        public bool PeekAnomalyIn(RoomId room)
        {
            if (HuntActive || (CurrentShiftDef != null && CurrentShiftDef.HasAdministrator)) return false;
            if ((Session.PeekAnomaliesGone & PatrolCycle.Bit(room)) != 0) return false;
            return PatrolCycle.StandingIn(Session.Seed, Session.CurrentShift, room);
        }

        /// <summary>The lamp drove out the anomaly in the room being peeked: it is gone for the night
        /// and the room counts for the patrol. </summary>
        public void RepelPeekAnomaly()
        {
            if (!PeekRoom.HasValue) return;
            int bit = PatrolCycle.Bit(PeekRoom.Value);
            Session.PeekAnomaliesGone |= bit;
            Session.ClearedRooms |= bit;
            ObjectiveText = PatrolObjective();
            Audio.PlaySfx("sfx_glitch", 0.6f);
            Bus.Publish(GameEvent.Of(EventIds.CaptionRequested, "*it folds into the dark and is gone*", 3f, null));
        }

        /// <summary>Normal nights 1 to 4 need one patrol cycle before END SHIFT.</summary>
        public bool PatrolRequired => Session != null && Session.Mode == GameMode.Normal &&
            Session.CurrentShift < ShiftPlan.TotalShifts;

        /// <summary>True once tonight's cycle is finished, or when tonight has none.</summary>
        public bool PatrolDone => !PatrolRequired || PatrolCycle.Done(Session.ClearedRooms);

        private string PatrolObjective()
        {
            if (!PatrolRequired) return "Night " + Session.CurrentShift + " - patrol: follow the chain, return to the checkpoint before dawn.";
            return PatrolDone ? "Patrol done - return to the desk." : "Patrol: " + PatrolCycle.Left(Session.ClearedRooms);
        }

        public static string MusicForRoom(RoomId room)
        {
            switch (room)
            {
                case RoomId.Classroom1: return "classroom";
                case RoomId.Classroom2: return "darkroom";
                default: return "hallway";
            }
        }

        /// <summary>Peek into a room: the player never walks inside. The camera frames the room
        /// and the lamp's hole in the dark reveals it. Every room is reached only this way.</summary>
        public void EnterRoomPeek(RoomId room)
        {
            if (Views == null) return;
            PeekRoom = room;
            if (!PeekAnomalyIn(room)) Session.ClearedRooms |= PatrolCycle.Bit(room);
            ObjectiveText = PatrolObjective();   // the hallway shows it again after the peek
            SetState(AppViewState.RoomPeek);
            Views.EnterPeek(room);
        }

        /// <summary>Step back from a peek into the hallway. ViewManager.ExitPeek runs the
        /// fade, clears PeekRoom and returns the state to Exploration itself.</summary>
        public void ExitRoomPeek()
        {
            if (Views == null) PeekRoom = null;
            else Views.ExitPeek();
        }

        public void ReturnToCheckpoint()
        {
            QueueSceneAction(() =>
            {
                SetState(AppViewState.Checkpoint);
                Audio.PlayMusic("checkpoint");
                if (Checkpoint != null) Checkpoint.ResumeRun();
            });
            Hollowbrook.Bootstrap.SceneRouter.Load(Hollowbrook.Bootstrap.SceneRouter.Checkpoint);
        }

        public void OpenMap()
        {
            if (MapScreen == null)
            {
                Bus.Publish(GameEvent.Of(EventIds.Notification, "The map is pinned up at the checkpoint."));
                return;
            }
            if (State != AppViewState.Checkpoint) return;   // the map is pinned up in the security office only
            SetState(AppViewState.Map);
            Audio.PlaySfx("sfx_ui_open");
        }

        public void CloseMap()
        {
            Audio.PlaySfx("sfx_ui_close");
            SetState(AppViewState.Checkpoint);
        }
    }
}
