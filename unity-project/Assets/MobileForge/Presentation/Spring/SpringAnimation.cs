using System;

namespace MobileForge.Presentation
{
    /// <summary>
    /// Damped harmonic oscillator for organic, springy motion.
    /// Works for float values — compose multiple for Vector2/Vector3.
    /// </summary>
    public class SpringAnimation
    {
        private float _value;
        private float _velocity;
        private float _target;
        private float _damping;
        private float _frequency;
        private float _threshold = 0.001f;

        public float Value => _value;
        public float Target => _target;
        public bool IsSettled => Math.Abs(_value - _target) < _threshold && Math.Abs(_velocity) < _threshold;

        public SpringAnimation(float initial = 0f, float damping = 0.5f, float frequency = 15f)
        {
            _value = initial;
            _target = initial;
            _damping = damping;
            _frequency = frequency;
        }

        /// <summary>Set the target value the spring moves toward.</summary>
        public void SetTarget(float target) => _target = target;

        /// <summary>Apply an impulse (add to velocity).</summary>
        public void Impulse(float force) => _velocity += force;

        /// <summary>Update the spring. Call each frame with delta.</summary>
        public float Update(float delta)
        {
            float omega = _frequency * MathF.PI * 2f;
            float dampingForce = 2f * _damping * omega;
            float springForce = omega * omega;

            float displacement = _value - _target;
            float acceleration = -springForce * displacement - dampingForce * _velocity;
            _velocity += acceleration * delta;
            _value += _velocity * delta;
            return _value;
        }

        /// <summary>Reset to a value with zero velocity.</summary>
        public void Reset(float value)
        {
            _value = value;
            _target = value;
            _velocity = 0f;
        }
    }
}
