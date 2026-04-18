using System;
using UnityEngine;

namespace JunkyardBoss
{
    public static class BossExcavatorMotionProfile
    {
        private const float MinAngle = 0.01f;
        private const float SimulationDeltaTime = 1f / 60f;
        private const int MaxSimulationSteps = 4096;

        public static Quaternion StepRotation(
            Quaternion currentRotation,
            Quaternion targetRotation,
            ref float currentSpeed,
            float maxSpeed,
            float acceleration,
            float deceleration,
            float slowAngle,
            float minSpeedFactor,
            float deltaTime)
        {
            ValidateParameters(maxSpeed, acceleration, deceleration, slowAngle, minSpeedFactor);

            float remainingAngle = Quaternion.Angle(currentRotation, targetRotation);

            if (remainingAngle <= MinAngle)
            {
                currentSpeed = 0f;

                return targetRotation;
            }

            float nextSpeed = GetNextSpeed(
                remainingAngle,
                currentSpeed,
                maxSpeed,
                acceleration,
                deceleration,
                slowAngle,
                minSpeedFactor,
                deltaTime);
            float step = nextSpeed * deltaTime;

            if (step >= remainingAngle)
            {
                currentSpeed = 0f;

                return targetRotation;
            }

            currentSpeed = nextSpeed;

            return Quaternion.RotateTowards(currentRotation, targetRotation, step);
        }

        public static float EstimateTravelTime(
            Quaternion currentRotation,
            Quaternion targetRotation,
            float currentSpeed,
            float maxSpeed,
            float acceleration,
            float deceleration,
            float slowAngle,
            float minSpeedFactor)
        {
            ValidateParameters(maxSpeed, acceleration, deceleration, slowAngle, minSpeedFactor);

            float remainingAngle = Quaternion.Angle(currentRotation, targetRotation);

            if (remainingAngle <= MinAngle)
            {
                return 0f;
            }

            float elapsedTime = 0f;
            float speed = currentSpeed;
            int stepIndex = 0;

            while (remainingAngle > MinAngle && stepIndex < MaxSimulationSteps)
            {
                stepIndex += 1;

                float nextSpeed = GetNextSpeed(
                    remainingAngle,
                    speed,
                    maxSpeed,
                    acceleration,
                    deceleration,
                    slowAngle,
                    minSpeedFactor,
                    SimulationDeltaTime);
                float step = nextSpeed * SimulationDeltaTime;
                elapsedTime += SimulationDeltaTime;

                if (step >= remainingAngle)
                {
                    return elapsedTime;
                }

                remainingAngle -= step;
                speed = nextSpeed;
            }

            return elapsedTime;
        }

        private static float GetNextSpeed(
            float remainingAngle,
            float currentSpeed,
            float maxSpeed,
            float acceleration,
            float deceleration,
            float slowAngle,
            float minSpeedFactor,
            float deltaTime)
        {
            float desiredSpeed = GetDesiredSpeed(remainingAngle, maxSpeed, slowAngle, minSpeedFactor);
            float speedChange = acceleration;

            if (currentSpeed > desiredSpeed)
            {
                speedChange = deceleration;
            }

            return Mathf.MoveTowards(currentSpeed, desiredSpeed, speedChange * deltaTime);
        }

        private static float GetDesiredSpeed(float remainingAngle, float maxSpeed, float slowAngle, float minSpeedFactor)
        {
            float clampedMinSpeedFactor = Mathf.Clamp01(minSpeedFactor);
            float minSpeed = maxSpeed * clampedMinSpeedFactor;

            if (remainingAngle >= slowAngle)
            {
                return maxSpeed;
            }

            float blend = Mathf.Clamp01(remainingAngle / slowAngle);

            return Mathf.Lerp(minSpeed, maxSpeed, blend);
        }

        private static void ValidateParameters(
            float maxSpeed,
            float acceleration,
            float deceleration,
            float slowAngle,
            float minSpeedFactor)
        {
            if (maxSpeed <= 0f)
            {
                throw new InvalidOperationException(nameof(maxSpeed));
            }

            if (acceleration <= 0f)
            {
                throw new InvalidOperationException(nameof(acceleration));
            }

            if (deceleration <= 0f)
            {
                throw new InvalidOperationException(nameof(deceleration));
            }

            if (slowAngle <= 0f)
            {
                throw new InvalidOperationException(nameof(slowAngle));
            }

            if (minSpeedFactor < 0f || minSpeedFactor > 1f)
            {
                throw new InvalidOperationException(nameof(minSpeedFactor));
            }
        }
    }
}
