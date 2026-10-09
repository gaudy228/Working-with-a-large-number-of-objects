using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

public class CircularMotionJob : MonoBehaviour
{
    [SerializeField] private GameObject _prefab;
    [SerializeField] private int _objectCount = 500;
    [SerializeField] private float _radius = 20f;
    [SerializeField] private float _angularSpeed = 1f;

    private TransformAccessArray _transformAccessArray;
    private NativeArray<float> _phases;
    private NativeArray<float> _logResults;
    private float _elapsedTime;
    private float _logTimer;
    private uint _logSeed = 1u;

    void Start()
    {
        Transform[] transforms = new Transform[_objectCount];
        _phases = new NativeArray<float>(_objectCount, Allocator.Persistent);
        _logResults = new NativeArray<float>(_objectCount, Allocator.Persistent);

        for (int i = 0; i < _objectCount; i++)
        {
            float angle = (float)i / _objectCount * 2f * math.PI;
            float3 position = new float3(math.cos(angle) * _radius, 0, math.sin(angle) * _radius);
            GameObject go = Instantiate(_prefab, position, Quaternion.identity);
            transforms[i] = go.transform;
            _phases[i] = angle;
        }

        _transformAccessArray = new TransformAccessArray(transforms);
    }

    void Update()
    {
        _elapsedTime += Time.deltaTime;

        var moveJob = new MoveInCircleJob
        {
            ElapsedTime = _elapsedTime,
            Radius = _radius,
            AngularSpeed = _angularSpeed,
            Phases = _phases
        };

        JobHandle moveHandle = moveJob.Schedule(_transformAccessArray);
        moveHandle.Complete();

        RunLogJob();
    }

    private void RunLogJob()
    {
        _logSeed = (uint)UnityEngine.Random.Range(1, int.MaxValue);

        var logJob = new LogRandomNumberJob
        {
            Results = _logResults,
            Seed = _logSeed
        };

        JobHandle logHandle = logJob.Schedule();
        logHandle.Complete();

        for (int i = 0; i < _logResults.Length; i++)
        {
            Debug.Log($"{i} - {_logResults[i]}");
        }
    }

    void OnDestroy()
    {
        if (_transformAccessArray.isCreated)
        {
            _transformAccessArray.Dispose();
        }
        if (_phases.IsCreated)
        {
            _phases.Dispose();
        }
        if (_logResults.IsCreated)
        {
            _logResults.Dispose();
        }
    }

    [Unity.Burst.BurstCompile]
    private struct MoveInCircleJob : IJobParallelForTransform
    {
        public float ElapsedTime;
        public float Radius;
        public float AngularSpeed;
        [ReadOnly] public NativeArray<float> Phases;

        public void Execute(int index, TransformAccess transform)
        {
            float angle = Phases[index] + ElapsedTime * AngularSpeed;
            float3 newPos = new float3(math.cos(angle) * Radius, 0, math.sin(angle) * Radius);
            transform.position = newPos;
        }
    }

    [Unity.Burst.BurstCompile]
    private struct LogRandomNumberJob : IJob
    {
        [WriteOnly] public NativeArray<float> Results;
        public uint Seed;

        public void Execute()
        {
            var random = new Unity.Mathematics.Random(Seed);

            for (int i = 0; i < Results.Length; i++)
            {
                float value = random.NextFloat(1f, 100f);
                Results[i] = math.log(value);
            }
        }
    }
}