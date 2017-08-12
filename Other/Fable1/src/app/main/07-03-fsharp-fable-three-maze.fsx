(*@
    Layout = "post";
    Title = "Fabled F# Mazes";
    Date = "2017-07-03T08:48:31";
    Tags = "fsharp threejs fable maze functional";
    Description = "Interactive maze generation using F#, Fable, ThreeJs and NOT javascript";
*)
(*** more ***)
(**

** Fabled F# _Maze_ **
----------------------

<div id="graphicsWrapper"><div id="graphicsContainer"></div></div>

<script src="http://cdnjs.cloudflare.com/ajax/libs/three.js/r77/three.js"></script>
<script src="/otherOutput/fable1/BlogFableThreeMazeBuild.js"></script>

**I love mazes, and I wanted to try and generate them in a webpage. Trouble was, maze generation was way too complicated for my 
javascript skills. But I was sure I could do better in a more idiot-proof language like F#...** Fortunately, now that F# can be 
compiled to javascript by the wonderful [Fable](http://fable.io), maze building in the browser is (just about) achievable for my 
simple mind. The **demo above** has been _built entirely_ from the [F#](http://fsharp.org) _code in this webpage_, using 
[Fable](http://fable.io). Graphics are courtesy of Fable's bindings to [ThreeJs](https://threejs.org). (You can also view the 
complete code on my [GitHub repo](https://github.com/codehutch/codehutch.github.io/blob/source/Other/Fable1/src/app/main/07-03-fsharp-fable-three-maze.fsx) 
if you prefer). As a disclaimer, nicer and shorter maze generation algorithms are available than the one described here, but this 
one does illustrate quite a few F# concepts...
   
### _**Building** blocks_ ###

So we're going to need to generate a maze, and then render it graphically. My 3D graphics skills are pretty much limited to drawing
[cubes](http://www.progletariat.com/blog/2017/06-22-fable-threejs-hello/index.html), so we'll base our representation of a maze
around single _cells_, that either do or don't get drawn as a cube. If a `Cell` is drawn as a cube, it's part of a wall in the maze. 
If a `Cell` is not drawn, then it's a gap or passage in the maze. So a `Cell` is effectively a thing that can exist as one of several
possible forms (but an individual `Cell` can only ever be one of these possible forms). In F#, a 
[discriminated union](https://fsharpforfunandprofit.com/posts/discriminated-unions/) type fits the bill when we need to encompass 
several independent possibilities within the same type. I'll use `X` to represent a `Cell` that is part of a maze-wall and `O` to 
represent a `Cell` that is open-space in the maze. I'll also throw in `I` to represent a `Cell` who's type is either as yet indeterminate 
or not important (more on that later when we generate mazes).

*)

#r "../../../packages/Fable.Core/lib/netstandard1.6/Fable.Core.dll"
#load "../../../node_modules/fable-import-three/Fable.Import.Three.fs"

open System
open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Fable.Import.Three

// Here's our union-type for representing a maze.
type Cell = 
  | X // X is going to represent a wall.
  | O // O is going to represent open space / gap / a passage.
  | I // I represents intederminate / unknown / don't care.

let o = O // Sneaky trick to get syntax-highlighting to show open space better.

(**

#### _**Bigger bits** and pieces_ ####

Now that we have established the `Cell` union type for representing the lowest level building blocks of our maze, we
need to move up a level. The simpest possible maze is a _3 x 3 grid_ of Cells. We'll call this a `SmallSquare`. I've 
used a [tuple](https://fsharpforfunandprofit.com/posts/tuples/) with 9 members to represent a `SmallSquare`. Arguably
I could have attempted to represent the _3x3-ness_ better using a 2d array or a 3-tuple of 3-tuples etc, but they 
would generally introduce more syntactic noise (which F# does a pretty good job of minimizing) in the form of additional
brackets etc, so I'll stick with a tuple. Although the syntax for creating a tuple is pretty minimal, the code will be
clearer (in our case of needing to represent 2-dimensional mazes) if we can omit tuple-syntax (brackets and commas) from 
the source. What we can do is create `ss` which is a [curried function](https://fsharpforfunandprofit.com/posts/currying/)
_that requires no commas or brackets around its arguments_ for creating a `SmallSquare`. When we are generating mazes, 
we will want to replace a 3 x 3 `SmallSquare` with an equivalent (but more complex) _5 x 5 grid_ of Cells which we'll
call `LargeSquare`. I've also added `ls` for creating a `LargeSquare` in a syntactically-minimal way (like `ss`).

*)

// SmallSquare is a 3x3 grid of Cells
type SmallSquare = Cell * Cell * Cell
                 * Cell * Cell * Cell
                 * Cell * Cell * Cell 

// ss gives us a nice-looking way to create a small square
// without needing to type lots, of, commas.
let ss a b c
       d e f 
       g h i : SmallSquare = a, b, c,
                             d, e, f,
                             g, h, i
                             
// LargeSquare is a 5x5 grid of Cells
type LargeSquare = Cell * Cell * Cell * Cell * Cell
                 * Cell * Cell * Cell * Cell * Cell
                 * Cell * Cell * Cell * Cell * Cell
                 * Cell * Cell * Cell * Cell * Cell
                 * Cell * Cell * Cell * Cell * Cell

// ls creates LargeSquares in a visually pleasing (i.e. comma-free) way.                 
let ls a b c d e
       f g h i j
       k l m n o
       p q r s t
       u v w x y : LargeSquare = a, b, c, d, e,
                                 f, g, h, i, j,
                                 k, l, m, n, o,
                                 p, q, r, s, t, 
                                 u, v, w, x, y

(**

### _**Flipping** functions_ ###

When we're generating mazes, we'll want to be able to come up with new squares, based on existing ones.
One of the ways we can derive a new square from an existing one is to _flip_ or _rotate_ the existing
square. We can achieve this pretty easily by taking each `Cell` from a `LargeSquare` and rearranging 
the cells as required. It's reasonably easy to see just by looking at the code how the flip or rotation
transforms the input square's cells. Whilst we're writing utility functions, we'll also include one to
convert the cells from a large square into F# [list](https://docs.microsoft.com/en-us/dotnet/fsharp/language-reference/lists) 
form, which will be useful later. As you can see, the F# syntax for creating a list is pretty minimal.

*)
        
// Flip a large square horizontally
let flip a b c d e
         f g h i j
         k l m n o
         p q r s t
         u v w x y = ls e d c b a
                        j i h g f
                        o n m l k
                        t s r q p
                        y x w v u

// Rotate a large square clockwise by 90 degrees
let rotate a b c d e
           f g h i j
           k l m n o
           p q r s t
           u v w x y = ls u p k f a
                          v q l g b
                          w r m h c
                          x s n i d
                          y t o j e

// Convert a large square to list representation 
let toList a b c d e
           f g h i j
           k l m n o
           p q r s t
           u v w x y = [a; b; c; d; e; 
                        f; g; h; i; j; 
                        k; l; m; n; o; 
                        p; q; r; s; t; 
                        u; v; w; x; y]

(**

#### _**Adapting** as necessary_ ####

Although the above functions are (in my eyes) quite elegant, we can't actually use them at the moment, 
as they take each cell _individually_ from a square as input, **but** we have defined `SmallSquare` and
`LargeSquare` as tuples, which group up 9 or 25 Cells into _singular entities_. To be able to `rotate` or
`flip` a square, we will need adapter function that takes a _nice-syntax_ function and an _ugly-tuple-syntax_
square then extracts each `Cell` from the square and then passes each `Cell` individually to the _nice-syntax_
function. As we will need to use these adapter functions an aweful lot, we'll make them 
[custom infix operators](https://docs.microsoft.com/en-us/dotnet/fsharp/language-reference/operator-overloading) so
that we can use them in an intuitive way (intuitive if you like that kind of thing, anyway). 

*)
       
// Adapt a 'nice-syntax' function (sf) to work with an actual SmallSquare.
let (|>>|) sf (a, b, c,
               d, e, f,
               g, h, i) = sf a b c
                             d e f
                             g h i
   
// Adapt a 'nice-syntax' function (sf) to work with an actual LargeSquare.
let (||>>||) sf (a, b, c, d, e,
                 f, g, h, i, j,
                 k, l, m, n, o,
                 p, q, r, s, t,
                 u, v, w, x, y) = sf a b c d e
                                     f g h i j
                                     k l m n o
                                     p q r s t
                                     u v w x y

(**

### _**All things being** equal_ ###

We need to be able to work out if a `Cell` is the same as another `Cell`. F# helps us out here by 
implementing [structural-equality](https://blogs.msdn.microsoft.com/dsyme/2009/11/08/equality-and-comparison-constraints-in-f/)
by default on tuple, union and record types. However, if you remember back to the initial definition
of the `Cell` type, we included `I` type cells to represent cases where the type of a cell can be
_indeterminate_ (i.e. when it doesn't matter if the `Cell` finally becomes an `X` or an `O`). So, we
need to define _equals_ (|=|) and not-equals (|<>|) operators for `Cell` type that take the 
unimportance of `I` cells into account. We can also scale _equals_ and _not-equals_ up to work on 
LargeSqaures. To do this we convert the squares in question to Lists (via our `||>>||` adapter
function / operator) and use the 
[List.fold2](https://msdn.microsoft.com/en-us/visualfsharpdocs/conceptual/list.fold2%5B't1,'t2,'state%5D-function-%5Bfsharp%5D) 
function to combine the results of comparing each `Cell`.

*)

// Equals operator for Cells
let (|=|) (a:Cell) (b:Cell) =
    match a, b with
    | X, I -> true
    | O, I -> true
    | I, X -> true
    | I, O -> true
    | _, _ -> a = b

// Not-Equals operator for Cells
let (|<>|) (a:Cell) (b:Cell) =
    match a, b with
    | I, _ -> false
    | _, I -> false
    | _, _ -> a <> b

// Equals operator for LargeSquares
let (||=||) (a:LargeSquare) (b:LargeSquare) =
    let al = toList ||>>|| a
    let bl = toList ||>>|| b
    List.fold2 (fun s x y -> s && (x |=| y)) true al bl

// Not-Equals operator for LargeSquares
let (||<>||) (a:LargeSquare) (b:LargeSquare) =
    let al = toList ||>>|| a
    let bl = toList ||>>|| b
    List.fold2 (fun s x y -> s || (x |<>| y)) false al bl

(**

#### _**I'll tell you what I** don't **want**_ ####

To generate mazes, we need a collection of all possible maze squares. We'll approach this by
generating all possible combinations of cells and then filtering out ones which aren't valid
for some reason (such as ones that contain loops or don't have entry/exit points). To avoid 
having to manually specify hundreds of invalid squares, we can write an `allRotationsAndFlips`
function that takes an (invalid) square and generates all possible rotated and flipped versions
of it (the idea being that if a square is invalid in one orientation then it is invalid in all
orientations). Our `allRotationsAndFlips` function accepts a `LargeSquare` to work on, but it
would also be nice if it accepted individual cells that would form a square, so `all` is an
adapter function for that. We can use `all` to help build up a `neverValid` list of squares that 
are not valid in any maze that we generate.

*)

let allRotationsAndFlips (s:LargeSquare) =
  let r0   = s 
  let r90  = rotate ||>>|| r0
  let r180 = rotate ||>>|| r90
  let r270 = rotate ||>>|| r180
  let f0   = flip   ||>>|| s
  let f90  = rotate ||>>|| f0
  let f180 = rotate ||>>|| f90
  let f270 = rotate ||>>|| f180
  [r0; r90; r180; r270; f0; f90; f180; f270]

// adapter function for allRotationsAndFlips
let all a b c d e 
        f g h i j
        k l m n o
        p q r s t
        u v w x y = allRotationsAndFlips <| ls a b c d e
                                               f g h i j
                                               k l m n o
                                               p q r s t
                                               u v w x y

// Using the ampersand operator for list.append is a problem
// for my blogging framework (this is a work-around). 
let (|.|) x y = List.append x y

// Specifies characteristics that make squares invalid
let neverValid = [ls  I I I I I 
                      I o o o I
                      I o I o I
                      I o o o I
                      I I I I I  ] // A loop isn't valid
                 |.|
                 all I I X I I 
                     I I X I I 
                     X X X I I 
                     I I I I I 
                     I I I I I     // Small closed section isn't valid
                 |.|    
                 all I I I I I
                     I I I I I 
                     X X X X X
                     I I I I I 
                     I I I I I     // Medium closed section isn't valid
                 |.|
                 [ls X X X X X
                     X I I I X
                     X I I I X
                     X I I I X
                     X X X X X  ]  // Full closed square isn't valid
                 |.|
                 all X o X o X
                     I I I I I 
                     I I I I I 
                     I I I I I 
                     I I I I I     // Two entry points on any side aren't valid
                 
// In a 5x5 cell - corners and certain dividing cells are always required.
let alwaysRequired = ls  X I X I X
                         I O I O I
                         X I X I X
                         I O I O I
                         X I X I X  // Standard structure boundary walls

(**

### _**Give me** everything_ ###

To actually generate all *valid* squares, we can write a [recursive](https://en.wikibooks.org/wiki/F_Sharp_Programming/Recursion)
function (`als`) that builds all possible squares (in list form) by starting with an empty list and adding both an `X` and `O` 
to the empty list and then (recursively) to each list that has already been generated. Rather than generate 25^2 candidate squares,
we note that in a valid (_orthodox_) square that wall-cells must always be present in each corner, in the center and in the mid-point
of each side. Similarly, certain cells always have to be open. This means that we only need to generate 12^2 candidates as less
than half the cells are actually variable within a large square. The `orthodoxLS` function enforces this principal - when given
the variable parts of a square as inputs. We build `allLargeSquares` by filtering 12^2 possible candidates and filtering out
ones matching `neverValid` squares with undesirable characteristics. I also put the squares into a set to eliminate any duplicates,
again this is helped by F# implementing [structural-equality](https://blogs.msdn.microsoft.com/dsyme/2009/11/08/equality-and-comparison-constraints-in-f/)
by default.

*)

let orthodoxLS [b; d; f; h; j; l; n; p; r; t; v; x] =
    ls  X b X d X
        f O h O j
        X l X n X
        p O r O t
        X v X x X

let allLargeSquares = 
  let rec als n b = 
    match n with
    | 0 -> b
    | n ->  als (n-1) <| ((List.map (fun a -> X :: a) b) |.| (List.map (fun a -> O :: a) b))
  let allInputs = als 12 [[]]
  List.map (fun l -> orthodoxLS l) allInputs
  |> List.filter (fun x -> List.fold (fun a y -> a && x ||<>|| y) true neverValid)
  |> List.filter (fun x -> x ||=|| alwaysRequired)
  |> Set.ofList
  |> Set.toArray

type SideReq = 
| TopReq of (Cell * Cell * Cell * Cell * Cell) option
| LeftReq of (Cell * Cell * Cell * Cell * Cell) option 

type Reqs = { Top : SideReq; Left : SideReq}

let random = new System.Random ();

let isMatch  a b c d e
             f g h i j
             k l m n o
             p q r s t
             u v w x y  a' b' c'
                        d' e' f'
                        g' h' i' 
            
            (topReq : SideReq)
            (leftReq : SideReq) =

  let l = ls a b c d e
             f g h i j
             k l m n o
             p q r s t
             u v w x y

  let sideOK ssCell lsCellA lsCellB = 
    if ssCell = X 
    then lsCellA = X && lsCellB = X 
    else lsCellA <> lsCellB

  let reqOK (req:SideReq) a'' b'' c'' d'' e'' = 
    match req with
    | TopReq None  
    | LeftReq None -> true
    | TopReq (Some (a''', b''', c''', d''', e'''))  
    | LeftReq (Some (a''', b''', c''', d''', e''')) -> 
        a'' = a''' && b'' = b''' && c'' = c''' && 
        d'' = d''' && e'' = e'''

  let topOK = sideOK b' b d
  let bottomOK = sideOK h' v x
  let leftOK = sideOK d' f p
  let rightOK = sideOK f' j t
  let topReqOK = reqOK topReq a b c d e
  let leftReqOK = reqOK leftReq a f k p u
  
  topOK && bottomOK && leftOK && rightOK && topReqOK && leftReqOK

let replace a b c
            d e f
            g h i topReq
                  leftReq = 
  
  let possibles = allLargeSquares 
                  |> Array.filter (fun s -> (isMatch ||>>|| s) a b c
                                                               d e f
                                                               g h i
                                                               topReq 
                                                               leftReq)
  let n = Array.length possibles
  let choice = int (double n * random.NextDouble ())

  Array.item choice possibles                                                                 

let getBottomAsTopReq a b c d e
                      f g h i j
                      k l m n o
                      p q r s t
                      u v w x y = TopReq (Some (u, v, w, x, y))

let getRightAsLeftReq a b c d e
                      f g h i j
                      k l m n o
                      p q r s t
                      u v w x y = TopReq (Some (e, j, o, t, y))



let decompose a b c d e 
              f g h i j
              k l m n o
              p q r s t
              u v w x y  = (ss a b c
                               f g h
                               k l m, ss c d e
                                         h i j
                                         m n o,
                            ss k l m
                               p q r
                               u v w, ss m n o
                                         r s t
                                         w x y)

type Maze = LargeSquare list list

let replaceSquare sq topReqL topReqR leftReqT leftReqB =
  let (tl, tr,
       bl, br) = decompose ||>>|| sq    
  let ntl = (replace |>>| tl) topReqL                        leftReqT
  let ntr = (replace |>>| tr) topReqR                        (getRightAsLeftReq ||>>|| ntl)
  let nbl = (replace |>>| bl) (getBottomAsTopReq ||>>|| ntl) leftReqB
  let nbr = (replace |>>| br) (getBottomAsTopReq ||>>|| ntr) (getRightAsLeftReq ||>>|| nbl)  
  (ntl, ntr,
   nbl, nbr)

let growMaze (lsll : Maze) =
  let rec grl output prevOutputRowReqs (lsll : LargeSquare list list) =
    match lsll with
    | [] -> output
    | row::tail ->  
      let folder ((upper, lower), leftReqT, leftReqB) s (topReqL, topReqR) =
        let (ntl, ntr,
             nbl, nbr) = replaceSquare s topReqL topReqR leftReqT leftReqB
        (upper |.| [ntl; ntr], lower |.| [nbl; nbr]), getRightAsLeftReq ||>>|| ntr, getRightAsLeftReq ||>>|| nbr
      let (newTop, newBottom), _, _ = List.fold2 folder (([],[]), LeftReq None, LeftReq None) row prevOutputRowReqs
      let prevRowReqs = newBottom 
                        |> List.mapi (fun i b -> i, getBottomAsTopReq ||>>|| b) 
                        |> List.pairwise 
                        |> List.filter (fun ((i, r), (j, s)) -> i % 2 = 0)
                        |> List.map (fun ((i, r), (j, s)) -> (r, s))
      let newOutput = output |.| [newTop; newBottom]
      grl newOutput prevRowReqs tail
  let dummyTopReqs = List.init (List.length <| List.item 0 lsll) (fun x -> (TopReq None, TopReq None))      
  grl [] dummyTopReqs lsll               
  
let renderCube (scene:Scene) xs xe ys ye =

    let size = xe - xs
    let cube = Three.BoxBufferGeometry(size, size, 0.25 * size)
    let matProps = createEmpty<Three.MeshLambertMaterialParameters>
    matProps.color <- Some (U2.Case2 "#9430B3")
    let mesh = Three.Mesh(cube, Three.MeshLambertMaterial(matProps))
    mesh.translateX (xs - size / 2.0) |> ignore
    mesh.translateY (ys - size / 2.0) |> ignore
    mesh.translateZ 0.0 |> ignore
    scene.add(mesh)

let renderSquareRow (scene:Scene) xs xe ys ye a b c d e = 
  let widthStep = (xe - xs) / 5.0
  if a = X then renderCube scene (xs + 0.0 * widthStep) (xs + 1.0 * widthStep) ys ye
  if b = X then renderCube scene (xs + 1.0 * widthStep) (xs + 2.0 * widthStep) ys ye
  if c = X then renderCube scene (xs + 2.0 * widthStep) (xs + 3.0 * widthStep) ys ye
  if d = X then renderCube scene (xs + 3.0 * widthStep) (xs + 4.0 * widthStep) ys ye
  if e = X then renderCube scene (xs + 4.0 * widthStep) (xs + 5.0 * widthStep) ys ye

let rec renderSquare (scene:Scene) tlx tly brx bry a b c d e
                                                   f g h i j
                                                   k l m n o
                                                   p q r s t
                                                   u v w x y = 
        let heightStep = (bry - tly) / 5.0
        renderSquareRow scene tlx brx (tly + 0.0 * heightStep) (tly + 1.0 * heightStep) a b c d e 
        renderSquareRow scene tlx brx (tly + 1.0 * heightStep) (tly + 2.0 * heightStep) f g h i j
        renderSquareRow scene tlx brx (tly + 2.0 * heightStep) (tly + 3.0 * heightStep) k l m n o
        renderSquareRow scene tlx brx (tly + 3.0 * heightStep) (tly + 4.0 * heightStep) p q r s t
        renderSquareRow scene tlx brx (tly + 4.0 * heightStep) (tly + 5.0 * heightStep) u v w x y    

let rec renderMazeRow (scene:Scene) tlx tly brx bry row = 
    let step = (brx - tlx) / (float <| List.length row)
    row |> 
    List.iteri (fun i sq -> 
      let fi = float i
      (renderSquare scene (tlx + fi * step) tly (tlx + (fi + 1.0) * step) bry) ||>>|| sq)

let rec renderMaze(scene:Scene) tlx tly brx bry maze = 
    let step = (bry - tly) / (float <| List.length maze)
    maze |> 
    List.iteri (fun i r -> 
      let fi = float i
      renderMazeRow scene tlx (tly + fi * step) brx (tly + (fi + 1.0) * step)  r)

let rec randomMaze n =
  match n with 
  | n when n > 1 -> growMaze (randomMaze (n - 1))
  | _ -> [[replace X X X
                   o o o 
                   X X X 
                   (TopReq None)
                   (LeftReq None)]]
               
(**

#### _**5:** Action_ ####

Finally we're there. We can create a Scene and initialise all required elements by calling
the functions we defined above. We return a 4-tuple of the 4 key graphics elements back to
the caller so that those elements can be used later on in rendering / animation. In-fact,
"the caller" is just the line of script at the bottom of the section, which creates top-level
bindings to each of the key graphics elements.

*)

let action() =

    let width () = Browser.window.innerWidth * 0.75;
    let height () = Browser.window.innerHeight * 0.5

    let scene = Three.Scene()
    scene.autoUpdate <- true

    let camera = Three.PerspectiveCamera(75.0, width() / height(), 0.01, 1000.0)

    camera.matrixAutoUpdate <- true
    camera.rotationAutoUpdate <- true

    let initLights () =
      
      scene.add(Three.AmbientLight(U2.Case2 "#3C3C3C", 1.0))

      let spotLight = Three.SpotLight(U2.Case2 "#FFFFFF")
      spotLight.position.set(-30., 60., 60.) |> ignore
      scene.add(spotLight)

    initLights ()

    let renderer = Three.WebGLRenderer()
    renderer.setClearColor("#0A1D2D")
    (renderer :> Three.Renderer).setSize(width(), height())

    let container = if Browser.document.getElementById("graphicsContainer") <> null
                    then Browser.document.getElementById("graphicsContainer")
                    else Browser.document.body

    container.innerHTML <- ""
    container.appendChild((renderer :> Three.Renderer).domElement) |> ignore
    
    let makeButton text difficulty cssClass =    

        let button = Browser.document.createElement("button")
        button.innerText <- text
        button.className <- cssClass

        let buttonClick (b : Browser.MouseEvent) =
          let maze = randomMaze difficulty  
          while(scene.children.Count > 0) do 
            scene.remove(scene.children.Item(0)) 
          initLights ()  
          camera.position.z <- 0.0
          renderMaze scene -1.025 1.15 1.275 -1.15  maze
          (Boolean() :> obj)

        button.onclick <- Func<_,_> buttonClick
        button

    let buttonContainer = Browser.document.createElement("div")
    buttonContainer.className <- "buttonContainer"
    container.appendChild(buttonContainer) |> ignore

    buttonContainer.appendChild(makeButton "Easy" 2 "yellowGreen") |> ignore
    buttonContainer.appendChild(makeButton "Medium" 3 "yellowOrange") |> ignore
    buttonContainer.appendChild(makeButton "Hard" 4 "blueViolet") |> ignore

    renderMaze scene -1.025 1.15 1.275 -1.15 (randomMaze 3) 
    renderer, scene, camera

let renderer, scene, camera = action()

(**

### _**Making it** move_ ###

So, as we're using the movies as an analogy, we actually ought to add some movement to the scene,
a spinning cube is going to be much more impressive than a static one. Each frame we rotate the
cube a little about each of its axes to make it appear to spin. The use of requestAnimationFrame
(rather than a loop) ensures that the animation is paused if the render's target element isn't
on screen.

*)

let cameraPositionZ = 2.0

let rec reqFrame (dt:float) =
    Browser.window.requestAnimationFrame(Func<_,_> animate) |> ignore
    if camera.position.z < cameraPositionZ 
    then camera.position.z <- camera.position.z + 0.25
    else camera.position.z <- cameraPositionZ
    renderer.render(scene, camera)
and animate (dt:float) =
    Browser.window.setTimeout(Func<_,_> reqFrame, 1000.0 / 20.0) |> ignore // aim for 20 fps

animate(0.0) // Start!

