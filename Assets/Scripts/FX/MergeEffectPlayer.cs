using UnityEngine;

/// <summary>
/// Merge success particle burst.
/// </summary>
public static class MergeEffectPlayer
{
    public static void PlayMergeBurst(Vector3 worldPosition)
    {
        GameObject go = new GameObject("MergeBurst");
        go.transform.position = worldPosition;

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystem.MainModule main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 0.35f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.55f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 3.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.11f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 64;
        main.gravityModifier = 0.25f;

        ParticleSystem.EmissionModule em = ps.emission;
        em.rateOverTime = 0;
        em.SetBursts(new[] { new ParticleSystem.Burst(0f, 32) });

        ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
        col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(new Color(1f, 0.92f, 0.35f), 0.45f),
                new GradientColorKey(new Color(1f, 0.5f, 0.2f), 1f)
            },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        col.color = grad;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.18f;

        ParticleSystemRenderer pr = go.GetComponent<ParticleSystemRenderer>();
        if (pr != null)
            pr.sortingOrder = 25;

        ps.Play();
        Object.Destroy(go, 1.2f);
    }
}
