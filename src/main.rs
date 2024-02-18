// The #-commented lines are hidden in Rustdoc but not in raw
// markdown rendering, and contain boilerplate code so that the
// code in the README.md is actually run as part of the test suite.

extern crate bulletproofs;
extern crate curve25519_dalek_ng;
extern crate merlin;
extern crate rand;

use curve25519_dalek_ng::ristretto::{CompressedRistretto, RistrettoPoint};
use curve25519_dalek_ng::traits::MultiscalarMul;
use rand::RngCore;
use rand::{rngs::OsRng, thread_rng};

use curve25519_dalek_ng::scalar::Scalar;

use merlin::Transcript;

use bulletproofs::r1cs::*;
use bulletproofs::{BulletproofGens, PedersenGens, RangeProof};
use sha2::{Digest, Sha256, Sha512};

use std::{fs, io};

fn pedersen_commitment(file_path: String) -> (RistrettoPoint, Scalar) {
    let mut file = fs::File::open(file_path).expect("Failed to open file.");
    let mut hasher = Sha512::new();

    let bytes_written = io::copy(&mut file, &mut hasher).expect("Failed to read from file.");
    let hash_bytes: [u8; 64] = hasher.finalize().into();
    let hash_scalar = Scalar::from_bytes_mod_order_wide(&hash_bytes);

    let pc_gens = PedersenGens::default();
    let blinding = Scalar::random(&mut thread_rng());

    (pc_gens.commit(hash_scalar, blinding), blinding)
}

fn prove_submission(file_path: String) -> RistrettoPoint {
    let (commitment1, blinding1) = pedersen_commitment(file_path);
    let (commitment2, blinding2) = pedersen_commitment(file_path);

    let bp_gens = BulletproofGens::new(256, 1);
    let pc_gens = PedersenGens::default();
    let transcript = Transcript::new(b"Bastjan er ikke sej alligevel.");

    let prover = Prover::new(&pc_gens, &mut transcript);
    let proof = prover.commit()

    let proof = RangeProof::prove_single(bp_gens, pc_gens, &mut transcript, blinding1.as_u64(), 64);
}

fn main() {
    let mut rng = thread_rng();
    let randomness1 = Scalar::random(&mut rng);
    let randomness2 = Scalar::random(&mut rng);

    let (commitment1, commitment2) = generate_commitments(&paper, randomness1, randomness2);
}
