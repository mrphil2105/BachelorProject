struct Submission {
    encryption_key: &[u8; 32],
}

impl Submission {
    pub fn new() -> Self {
        Self {}
    }
}
