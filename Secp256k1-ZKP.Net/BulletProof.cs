﻿using System;
using static Secp256k1_ZKP.Net.Secp256k1Native;
using static Secp256k1_ZKP.Net.BulletProofNative;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Secp256k1_ZKP.Net
{
    public class BulletProof : IDisposable
    {
        public IntPtr Context { get; private set; }

        public BulletProof()
        {
            Context = secp256k1_context_create((uint)(Flags.SECP256K1_CONTEXT_SIGN | Flags.SECP256K1_CONTEXT_VERIFY));
        }

        public IntPtr Generators()
        {
            return secp256k1_bulletproof_generators_create(Context, Constant.GENERATOR_G, 256);
        }

        public ProofStruct ProofSingle(ulong value, byte[] blind, byte[] nonce, byte[] extraCommit, byte[] msg)
        {
            byte[] proof = new byte[Constant.MAX_PROOF_SIZE];
            int plen = Constant.MAX_PROOF_SIZE;

            byte[] tau_x = null;
            byte[] t_one = null;
            byte[] t_two = null;
            byte[] commits = null;
            byte[] privateNonce = null;

            var blinds = new IntPtr[1];

            IntPtr ptr = Marshal.AllocHGlobal(32);
            Marshal.Copy(blind, 0, ptr, blind.Length);
            blinds[0] = ptr;

            IntPtr[] values = new IntPtr[1];
            values[0] = (IntPtr)value;

            var gens = Generators();
            var scratch = secp256k1_scratch_space_create(Context, Constant.SCRATCH_SPACE_SIZE);
            var result = secp256k1_bulletproof_rangeproof_prove(
                            Context,
                            scratch,
                            gens,
                            proof,
                            ref plen,
                            tau_x,
                            t_one,
                            t_two,
                            values,
                            IntPtr.Zero,
                            blinds,
                            commits,
                            1,
                            Constant.GENERATOR_H,
                            64,
                            nonce,
                            privateNonce,
                            extraCommit,
                            0,
                            msg);

            if (result == 1)
            {
                Array.Resize(ref proof, plen);
            }

            _ = secp256k1_scratch_space_destroy(scratch);

            return new ProofStruct(proof, (uint)plen);
        }

        public ProofInfoStruct Rewind()
        {
            return new ProofInfoStruct();
        }

        public bool Verify(byte[] commit, byte[] proof, byte[] extraCommit)
        {
            var gens = Generators();
            var scratch = secp256k1_scratch_space_create(Context, Constant.SCRATCH_SPACE_SIZE);

            bool success = secp256k1_bulletproof_rangeproof_verify(
                            Context,
                            scratch,
                            gens,
                            proof,
                            proof.Length,
                            IntPtr.Zero,
                            commit,
                            1,
                            64,
                            Constant.GENERATOR_H,
                            extraCommit,
                            0) == 1;

            _ = secp256k1_scratch_space_destroy(scratch);

            return success;
        }

        public void Dispose()
        {
            if (Context != IntPtr.Zero)
            {
                secp256k1_context_destroy(Context);
                Context = IntPtr.Zero;
            }
        }
    }
}
