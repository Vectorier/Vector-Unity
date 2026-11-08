using System.Collections.Generic;
using Nekki.Vector.Core.Camera;
using Nekki.Vector.Core.Models;
using Nekki.Vector.Core.Transformation;
using Nekki.Vector.Core.Trigger;
using Nekki.Vector.Core.Trigger.Events;
using Nekki.Vector.Core.User;
using Nekki.Vector.Core.Visual;
using UnityEngine;
using ModelHuman = Nekki.Vector.Core.Models.ModelHuman;

namespace Nekki.Vector.Core.Location
{
    public class Location
    {
        private readonly Grid.Grid _Grid = new Grid.Grid();

        private readonly List<QuadRunner> _culledQuads = new List<QuadRunner>();

        private readonly List<QuadRunner> belongQuads = new List<QuadRunner>();

        private readonly TransformManager _transformManager;

        public List<ModelHuman> Models { get; } = new List<ModelHuman>();

        public string Music => Sets.Music;

        public List<UserData> UserData => Sets.UserData;

        public Sets Sets { get; }

        public Grid.Grid Grid => _Grid;

        public TransformManager transformManager => _transformManager;

        public int PointsOnLocation => Sets.totalPoints;

        public int BonusOnLocation => Sets.totalBonus;

        public int TrickOnLocation => Sets.totalTricks;

        /// <summary>Loads the level <paramref name="fileName"/> (".xml" optional) from <paramref name="xmlPath"/>.</summary>
        public Location(string xmlPath, string fileName)
        {
            if (!fileName.EndsWith(".xml"))
            {
                fileName += ".xml";
            }
            var doc = XmlUtils.OpenXMLDocument(xmlPath, fileName);
            if (doc == null)
            {
                throw new System.IO.FileNotFoundException("Can't open location file " + fileName);
            }
            Sets = new Sets(doc);
            _Grid.InitGrid(Sets.QuadsAll);
            _transformManager = new TransformManager();
        }

        public void CreateModelHuman(UserData userData)
        {
            var model = new ModelHuman(userData);
            model.Layer = Sets.Containers[1].Object;
            Models.Add(model);
            model.Start(GetSpawnByName(model.BirthSpawn));
        }

        public void Start()
        {
            Vector3d pos = new Vector3d(0, 0, 0);
            if (Sets.Cameras.Count > 0)
            {
                pos = new Vector3d(Sets.Cameras[0].Position);
            }
            LocationCamera.Current.StartPosition(pos);
            LocationCamera.Current.UpdatePosition();
            TE_StartGame.ActivateThisEvent();
        }

        public void Render()
        {
            _transformManager.Update();
            RenderModels();
            RenderElements();
            _transformManager.RemoveEndedTransformation();
        }

        public void RenderModels()
        {
            TriggerActionsRenderer.Current.Render();
            for (int i = 0; i < Models.Count; i++)
            {
                Models[i].ControllerAnimations.VelocityQuads();
                Models[i].Render();
                Collision(Models[i]);
            }
        }

        public void RenderElements()
        {
            foreach (var primitive in Sets.Primitives)
            {
                primitive.Render(Sets.Quads);
            }
            RunnerRender.RenderRunners();
        }

        public void Collision(ModelHuman p_modelHuman)
        {
            p_modelHuman.fixRectangle = true;
            _culledQuads.Clear();
            _Grid.Collect(p_modelHuman.Rectangle, _culledQuads);
            belongQuads.Clear();
            var belongs = p_modelHuman.ControllerCollisions.ControllerPlatforms.Belongs;
            for (int i = belongs.Count - 2; i > 0; i--)
            {
                belongQuads.Add(belongs[i].Platform);
            }
            foreach (var quad in belongQuads)
            {
                _culledQuads.Remove(quad);
                p_modelHuman.ControllerCollisions.UpdateQuad(quad);
            }
            foreach (var quad1 in _culledQuads)
            {
                p_modelHuman.ControllerCollisions.UpdateQuad(quad1);
            }
            foreach (var item in Sets.Items)
            {
                item.Collision(p_modelHuman);
            }
            foreach (var primitive in Sets.Primitives)
            {
                if (!primitive.IsStrike)
                {
                    p_modelHuman.ControllerCollisions.UpdatePrimitive(primitive);
                }
            }
            foreach (var model in Models)
            {
                p_modelHuman.ControllerCollisions.CheckModel(model);
            }
            p_modelHuman.fixRectangle = false;
        }

        public void Reload()
        {
            RunnerRender.Reset();
            TriggerActionsRenderer.Reset();
            ReloadLayers();
            ReloadModels();
            _transformManager.Clear();
            foreach (var runner in Sets.AllRunners)
            {
                runner.Reset();
            }
        }

        private void ReloadLayers()
        {
            _Grid.Reset();
        }

        private void ReloadModels()
        {
            foreach (var model in Models)
            {
                model.Reset();
                model.Start(GetSpawnByName(model.BirthSpawn));
            }
        }

        public SpawnRunner GetSpawnByName(string name)
        {
            foreach (var spawn in Sets.Spawns)
            {
                if (spawn.Name == name)
                {
                    return spawn;
                }
            }
            return null;
        }

        public ModelHuman GetUserModel()
        {
            foreach (var model in Models)
            {
                if (model.IsSelf)
                {
                    return model;
                }
            }
            return null;
        }

        public ModelHuman GetBotModel()
        {
            foreach (var model in Models)
            {
                if (model.IsBot)
                {
                    return model;
                }
            }
            return null;
        }

        public List<ModelHuman> GetAllBotModels()
        {
            List<ModelHuman> bots = new List<ModelHuman>();
            foreach (var model in Models)
            {
                if (model.IsBot)
                {
                    bots.Add(model);
                }
            }
            return bots;
        }

        public ModelHuman GetModelByName(string name)
        {
            foreach (var model in Models)
            {
                if (model.Name == name)
                {
                    return model;
                }
            }
            return null;
        }

        public List<CoinRunner> GetAllCoins()
        {
            List<CoinRunner> coinRunners = new List<CoinRunner>();
            foreach (var item in Sets.Items)
            {
                if (item is CoinRunner coin)
                {
                    coinRunners.Add(coin);
                }
            }
            return coinRunners;
        }

        public void RefreshTricks()
        {
            foreach (var area in Sets.Areas)
            {
                if (area is TrickAreaRunner trick)
                {
                    trick.Refresh();
                }
            }
        }
    }
}
