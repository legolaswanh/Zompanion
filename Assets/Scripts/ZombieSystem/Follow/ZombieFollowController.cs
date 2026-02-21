using System.Collections.Generic;
using UnityEngine;

public class ZombieFollowController : MonoBehaviour
{
    [SerializeField] [Min(0.01f)] private float sampleSpacing = 0.12f;
    [SerializeField] [Min(32)] private int maxTrailPoints = 512;
    [SerializeField] [Min(1)] private int maxFollowers = 2;

    private Transform _leader;
    private readonly List<ZombieAgent> _followers = new List<ZombieAgent>();
    private readonly Dictionary<Transform, List<Vector3>> _trailByTransform = new Dictionary<Transform, List<Vector3>>();

    public void SetLeader(Transform leader)
    {
        _leader = leader;
        if (_leader != null)
            EnsureTrail(_leader);
    }

    public void SetFollowers(IReadOnlyList<ZombieAgent> followers)
    {
        _followers.Clear();
        if (followers == null) return;

        for (int i = 0; i < followers.Count && i < maxFollowers; i++)
        {
            ZombieAgent agent = followers[i];
            if (agent == null) continue;
            _followers.Add(agent);
            EnsureTrail(agent.transform);
        }
    }

    private void LateUpdate()
    {
        if (_leader == null || _followers.Count == 0) return;

        CleanupInvalidFollowers();
        if (_followers.Count == 0) return;

        RecordTrailPoint(_leader);

        for (int i = 0; i < _followers.Count; i++)
        {
            ZombieAgent follower = _followers[i];
            if (follower == null) continue;

            Transform target = i == 0 ? _leader : _followers[i - 1].transform;
            List<Vector3> targetTrail = EnsureTrail(target);

            float followDistance = 0.75f;
            float followSpeed = 2.8f;
            if (follower.Definition != null)
            {
                followDistance = follower.Definition.FollowDistance;
                followSpeed = follower.Definition.FollowMoveSpeed;
            }

            FollowTrailPoint(follower, targetTrail, followDistance, followSpeed);
            RecordTrailPoint(follower.transform);
        }
    }

    private void FollowTrailPoint(ZombieAgent follower, List<Vector3> targetTrail, float followDistance, float speed)
    {
        if (targetTrail == null || targetTrail.Count == 0) return;

        int delaySamples = Mathf.Max(1, Mathf.CeilToInt(followDistance / sampleSpacing));
        if (targetTrail.Count <= delaySamples) return;

        int index = targetTrail.Count - 1 - delaySamples;
        Vector3 targetPosition = targetTrail[index];
        follower.MoveTowards(targetPosition, speed);
    }

    private List<Vector3> EnsureTrail(Transform target)
    {
        if (target == null) return null;

        if (!_trailByTransform.TryGetValue(target, out List<Vector3> trail))
        {
            trail = new List<Vector3>(maxTrailPoints);
            _trailByTransform[target] = trail;
            trail.Add(target.position);
        }
        return trail;
    }

    private void RecordTrailPoint(Transform target)
    {
        List<Vector3> trail = EnsureTrail(target);
        if (trail == null) return;

        Vector3 currentPos = target.position;
        if (trail.Count == 0)
        {
            trail.Add(currentPos);
            return;
        }

        if ((currentPos - trail[trail.Count - 1]).sqrMagnitude < sampleSpacing * sampleSpacing)
            return;

        trail.Add(currentPos);
        if (trail.Count > maxTrailPoints)
            trail.RemoveAt(0);
    }

    private void CleanupInvalidFollowers()
    {
        for (int i = _followers.Count - 1; i >= 0; i--)
        {
            if (_followers[i] != null) continue;
            _followers.RemoveAt(i);
        }
    }
}

