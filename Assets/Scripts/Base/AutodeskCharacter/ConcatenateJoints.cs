using System.Collections.Generic;
using animationparameters;
using UnityEngine;

namespace autodeskcharacter
{
    public class ConcatenateJoints
    {
        private readonly List<Concatenator> concatenators;
        private List<JointType> currentJointUsed;

        public ConcatenateJoints()
        {
            concatenators = new List<Concatenator>(JointType.NUMJOINTS);
            currentJointUsed = new List<JointType>();
            foreach (var type in JointType.values)
            {
                concatenators.Add(new IdentityConcatenator(type));
                currentJointUsed.Add(type);
            }
        }

        public static Vector3 toEulerXYZ(Quaternion q)
        {
            var r11 = 2 * (q.x * q.y + q.w * q.z);
            var r12 = q.w * q.w + q.x * q.x - q.y * q.y - q.z * q.z;
            var r21 = -2 * (q.x * q.z - q.w * q.y);
            var r31 = 2 * (q.y * q.z + q.w * q.x);
            var r32 = q.w * q.w - q.x * q.x - q.y * q.y + q.z * q.z;
            return new Vector3(Mathf.Atan2(r31, r32), Mathf.Asin(r21), Mathf.Atan2(r11, r12));
        }

        public static Quaternion fromXYZ(Vector3 xyz)
        {
            return fromXYZ(xyz.x, xyz.y, xyz.z);
        }

        public static Quaternion fromXYZ(float x, float y, float z)
        {
            var rx = x / 2.0f;
            var ry = y / 2.0f;
            var rz = z / 2.0f;
            var sinx = Mathf.Sin(rx);
            var siny = Mathf.Sin(ry);
            var sinz = Mathf.Sin(rz);
            var cosx = Mathf.Cos(rx);
            var cosy = Mathf.Cos(ry);
            var cosz = Mathf.Cos(rz);
            var qw = cosx * cosy * cosz + sinx * siny * sinz; // w
            var qx = sinx * cosy * cosz - cosx * siny * sinz; // x
            var qy = cosx * siny * cosz + sinx * cosy * sinz; // y
            var qz = cosx * cosy * sinz - sinx * siny * cosz; // z

            return new Quaternion(qx, qy, qz, qw);
        }

        public void setJointToUse(List<JointType> toUse)
        {
            concatenators.Clear();
            currentJointUsed = toUse;
            foreach (var type in toUse)
            {
                // only the spine and the chains sternum-to-shoulder can be concatanated
                //sternum-to-shoulder :
                if (type == JointType.r_shoulder)
                    if (!toUse.Contains(JointType.r_acromioclavicular))
                    {
                        if (toUse.Contains(JointType.r_sternoclavicular))
                            concatenators.Add(new ChainConcatenator(JointType.r_acromioclavicular, type));
                        else
                            concatenators.Add(new ChainConcatenator(JointType.r_sternoclavicular, type));
                        continue;
                    }

                if (type == JointType.r_acromioclavicular)
                    if (!toUse.Contains(JointType.r_sternoclavicular))
                    {
                        concatenators.Add(new AdditionConcatenator(JointType.r_sternoclavicular, type));
                        continue;
                    }

                if (type == JointType.l_shoulder)
                    if (!toUse.Contains(JointType.l_acromioclavicular))
                    {
                        if (toUse.Contains(JointType.l_sternoclavicular))
                            concatenators.Add(new ChainConcatenator(JointType.l_acromioclavicular, type));
                        else
                            concatenators.Add(new ChainConcatenator(JointType.l_sternoclavicular, type));
                        continue;
                    }

                if (type == JointType.l_acromioclavicular)
                    if (!toUse.Contains(JointType.l_sternoclavicular))
                    {
                        concatenators.Add(new AdditionConcatenator(JointType.l_sternoclavicular, type));
                        continue;
                    }

                //spine :
                if (isSpine(type))
                {
                    var parent = type;
                    while (parent.parent != JointType.HumanoidRoot && !toUse.Contains(parent.parent))
                        parent = parent.parent;
                    if (parent != type)
                    {
                        concatenators.Add(new ChainConcatenator(parent, type));
                        continue;
                    }
                }

                concatenators.Add(new IdentityConcatenator(type));
            }
        }

        public AnimationParametersFrame concatenateJoints(AnimationParametersFrame frame)
        {
            var result = new AnimationParametersFrame(frame.APVector.Count, frame.getFrameNumber());
            foreach (var c in concatenators) c.concatenate(frame, result);
            return result;
        }

        public List<JointType> getJointToUse()
        {
            return new List<JointType>(currentJointUsed);
        }

        protected static Quaternion getJointRotation(AnimationParametersFrame frame, JointType joint)
        {
            var q = fromXYZ(
                joint.rotationX == BAPType.null_bap ? 0 : (float) frame.getRadianValue(joint.rotationX),
                joint.rotationY == BAPType.null_bap ? 0 : (float) frame.getRadianValue(joint.rotationY),
                joint.rotationZ == BAPType.null_bap ? 0 : (float) frame.getRadianValue(joint.rotationZ));
            return q;
        }

        protected static Quaternion computeChain(AnimationParametersFrame frame, JointType jointFrom, JointType jointTo)
        {
            var qTo = getJointRotation(frame, jointTo);
            if (jointFrom == jointTo) return qTo;
            return computeChain(frame, jointFrom, jointTo.parent) * qTo;
        }

        private static bool isSpine(JointType joint)
        {
            return isSpine_(joint, JointType.skullbase);
        }

        private static bool isSpine_(JointType joint, JointType vertebra)
        {
            if (vertebra == JointType.HumanoidRoot || vertebra == JointType.null_joint || vertebra == null)
                return false;
            if (joint == vertebra) return true;
            return isSpine_(joint, vertebra.parent);
        }

        private class Concatenator
        {
            public virtual void concatenate(AnimationParametersFrame original, AnimationParametersFrame result)
            {
            }
        }

        private class IdentityConcatenator : Concatenator
        {
            private readonly JointType joint;

            public IdentityConcatenator(JointType joint)
            {
                this.joint = joint;
            }

            public override void concatenate(AnimationParametersFrame original, AnimationParametersFrame result)
            {
                copy(original, result, joint.rotationX);
                copy(original, result, joint.rotationY);
                copy(original, result, joint.rotationZ);
            }

            private void copy(AnimationParametersFrame original, AnimationParametersFrame result, BAPType bapType)
            {
                if (bapType != BAPType.null_bap)
                    if (original.getMask(bapType))
                        result.applyValue(bapType, original.getValue(bapType));
            }
        }

        private class ChainConcatenator : Concatenator
        {
            private readonly JointType from;
            private readonly JointType to;

            public ChainConcatenator(JointType from, JointType to)
            {
                this.from = from;
                this.to = to;
            }

            public override void concatenate(AnimationParametersFrame original, AnimationParametersFrame result)
            {
                var xyz = toEulerXYZ(computeChain(original, from, to));
                if (to.rotationX != BAPType.null_bap) result.setRadianValue(to.rotationX, xyz.x);
                if (to.rotationY != BAPType.null_bap) result.setRadianValue(to.rotationY, xyz.y);
                if (to.rotationZ != BAPType.null_bap) result.setRadianValue(to.rotationZ, xyz.z);
            }
        }

        private class ChainConcatenator2 : Concatenator
        {
            private readonly JointType from;
            private readonly JointType to;

            public ChainConcatenator2(JointType from, JointType to)
            {
                this.from = from;
                this.to = to;
            }

            public override void concatenate(AnimationParametersFrame original, AnimationParametersFrame result)
            {
                var xyz = toEulerXYZ(computeChain(original, from, to));
                if (from.rotationX != BAPType.null_bap) result.setRadianValue(from.rotationX, xyz.x);
                if (from.rotationY != BAPType.null_bap) result.setRadianValue(from.rotationY, xyz.y);
                if (from.rotationZ != BAPType.null_bap) result.setRadianValue(from.rotationZ, xyz.z);
            }
        }

        private class AdditionConcatenator : Concatenator
        {
            private readonly JointType from;
            private readonly JointType to;

            public AdditionConcatenator(JointType from, JointType to)
            {
                this.from = from;
                this.to = to;
            }

            public override void concatenate(AnimationParametersFrame original, AnimationParametersFrame result)
            {
                var addition = new int[3];
                var joint = to;
                while (joint != from)
                {
                    add(addition, original, joint);
                    joint = joint.parent;
                }

                add(addition, original, from);

                if (to.rotationX != BAPType.null_bap) result.applyValue(to.rotationX, addition[0]);
                if (to.rotationY != BAPType.null_bap) result.applyValue(to.rotationY, addition[1]);
                if (to.rotationZ != BAPType.null_bap) result.applyValue(to.rotationZ, addition[2]);
            }

            private void add(int[] addition, AnimationParametersFrame frame, JointType joint)
            {
                if (joint.rotationX != BAPType.null_bap && frame.getMask(joint.rotationX))
                    addition[0] = addition[0] + frame.getValue(joint.rotationX);
                if (joint.rotationY != BAPType.null_bap && frame.getMask(joint.rotationY))
                    addition[1] = addition[1] + frame.getValue(joint.rotationY);
                if (joint.rotationZ != BAPType.null_bap && frame.getMask(joint.rotationZ))
                    addition[2] = addition[2] + frame.getValue(joint.rotationZ);
            }
        }
    }
}