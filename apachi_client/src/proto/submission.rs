use rand::rngs::OsRng;

pub struct Submission {
    encryption_key: &[u8; 32],
    rand_submitter: u32,
    rand_reviewer: u32,
}

impl Submission {
    pub fn new() -> Self {
        let rng = OsRng::new().expect("Failed to create OsRng instance.");
        let encryption_key = [0u8; 32];
        let rand_submitter = 10;
        let rand_reviewer = 3;

        Self {
            encryption_key: &encryption_key,
            rand_submitter,
            rand_reviewer,
        }
    }
}
