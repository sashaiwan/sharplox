# C# Lox Interpreter
This is a C# implementation of the Lox programming language, based on Bob Nystrom's book Crafting Interpreters. Lox is a dynamically typed scripting language with a clean, familiar syntax.
Features:

- Dynamic typing
- First-class functions and closures
- Classes and inheritance
- Lambda expressions
- Automatic memory management
- Static semantic analyzer
- REPL (Read-Eval-Print Loop) support

## Personal Journey
I've taken on several of the book's challenges along this
implementation journey. While some challenges were 
straightforward and relatively easy to implement, 
others proved more challenging. Even after long 
debugging sessions, some required a rollback when the 
bugs proved too elusive. Far from seeing these as 
failures, I strongly recommend tackling even the challenges 
you don't fully understand - it's been a real game-changer 
in my learning experience.

## Getting started
### Prerequisites
- .NET 8.0 SDK

### Installation

Clone the repository:
```bash
git clone https://github.com/sashaiwan/sharplox.git
cd SharpLox
```

### Build the project:
```bash
dotnet build
```

### Running the Interpreter
#### REPL Mode
To start the interactive REPL:
```bash
dotnet run --project SharpLox
```

#### File Mode
To execute a Lox script file:
```bash
dotnet run --project SharpLox path/to/your/script.lox
```

## Language examples
### Hello World
```lox
print "Hello, world!";
```

### Variables and Control Flow
```lox
var a = 1;
var b = 2;
if (a < b) {
    print "b is bigger!";
} else {
    print "a is bigger!";
}
```
### Functions
```lox
fun fibonacci(n) {
    if (n <= 1) return n;
    return fibonacci(n - 2) + fibonacci(n - 1);
}

print fibonacci(10);
```

### Classes
```lox
class Animal {
    init(name) {
        this.name = name;
    }
    speak() {
        print this.name + " makes a sound.";
    }
}

class Dog < Animal {
    speak() {
        print this.name + " barks.";
    }
}

var dog = Dog("Rover");
dog.speak();
```

## Implementation Details
This implementation follows the tree-walk interpreter approach described in Part II of Crafting Interpreters. Key differences from the Java implementation include:

- Use of C# idioms and conventions
- Modern C# features utilization

## Contributing
Contributions are welcome! Please feel free to submit a Pull Request. For major changes, please open an issue first to discuss what you would like to change.

## Testing
To run the test suite: `dotnet test`

## License
This project is licensed under the MIT License - see the LICENSE file for details.

## Acknowledgments
- Bob Nystrom for the excellent Crafting Interpreters book (and all the funny food references)
- The Lox language design
- The C# community for tools and support

Contact
If you have any questions or suggestions, please open an issue in the repository.