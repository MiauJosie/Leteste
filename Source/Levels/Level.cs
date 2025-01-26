using System.Collections.Generic;
using System.Linq;
using Leteste.Physics;

namespace Leteste.Levels;

public class Level
{
    private List<Actor> actors = new();
    private List<Solid> solids = new();

    public void AddActor(Actor actor) => actors.Add(actor);
    public void AddSolid(Solid solid) => solids.Add(solid);
    public void RemoveActor(Actor actor) => actors.Remove(actor);
    public List<Actor> GetActors() => actors;
    public List<Solid> GetSolids() => solids;

    public void Update()
    {
        foreach (var actor in actors.ToList())
        {
            actor.Update();
        }
    }

    public void Draw()
    {
        foreach (var solid in solids)
        {
            solid.Draw();
        }
        foreach (var actor in actors)
        {
            actor.Draw();
        }
    }
}