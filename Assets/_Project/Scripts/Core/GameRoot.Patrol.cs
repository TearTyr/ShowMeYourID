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
            QueueSceneAction(() =>
            {
                SetState(AppViewState.Exploration);
                Bus.Publish(GameEvent.Of(EventIds.PatrolStarted));
                ObjectiveText = "Night " + Session.CurrentShift + " - patrol: check the rooms, follow the chain, return to the checkpoint before dawn.";
                Bus.Publish(GameEvent.Of(EventIds.ObjectiveChanged, ObjectiveText));
                if (Views != null) Views.EnterRoom(room);
                Audio.PlayMusic(MusicForRoom(room));
                if (HuntActive && Views != null) Views.StartAdminHunt(BuildHuntTuning(), Session.Seed ^ 0x4A0B);
            });
            Hollowbrook.Bootstrap.SceneRouter.Load(Hollowbrook.Bootstrap.SceneRouter.School);
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

        /// <summary>Flashlight peek into a classroom: the player never walks inside - the
        /// camera frames the room and a cursor-locked light circle reveals it. Classrooms
        /// are reachable ONLY this way; the hallway and office keep their normal paths.</summary>
        public void EnterRoomPeek(RoomId room)
        {
            if (Views == null) return;
            PeekRoom = room;
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
                Bus.Publish(GameEvent.Of(EventIds.PatrolEnded));
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
