struct Submission {
    encryption_key: &[u8; 32],
    rand_submitter: u32,
    rand_reviewer: u32,
}

impl Submission {
    pub fn new() -> Self {
        Self {}
    }
}
