use rand::Rng;
use std::{cmp::Ordering, io};

fn main() {
    println!("Guess the number!");

    let number = rand::thread_rng().gen_range(1..=100);

    loop {
        let mut input = String::new();
        io::stdin()
            .read_line(&mut input)
            .expect("Unable to read input.");
        let guess: u32 = match input.trim().parse() {
            Ok(num) => num,
            Err(_) => {
                println!("Please type a number.");
                continue;
            }
        };

        match guess.cmp(&number) {
            Ordering::Less => println!("Your guess is too small."),
            Ordering::Greater => println!("Your guess is too big."),
            Ordering::Equal => {
                println!("You win!");
                break;
            }
        }
    }
}
