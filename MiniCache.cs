using System;//console ,math, basic
using System.Collections.Generic;//collections of list,stack,queue,etc.
using System.Linq;//first(),toarray(),last

public class MiniCache
{
    //Stores the maximum number of pages in frames
    private readonly int capacity;
    //Stores reference to web server for broadcasting
    private readonly WebServer server;
    //Creates different replacement algorithm
    private readonly ReplacementAlgo fifo;
    private readonly ReplacementAlgo lru;
    private readonly ReplacementAlgo clock;
    private readonly ReplacementAlgo optimal;

    public MiniCache(int capacity, WebServer server)
    {
        //Stores the capacity and server passed by the user
        this.capacity = capacity;
        this.server = server;
        //Creates 4 algorithm objects
        fifo = new ReplacementAlgo(capacity, AlgoType.FIFO);
        lru = new ReplacementAlgo(capacity, AlgoType.LRU);
        clock = new ReplacementAlgo(capacity, AlgoType.CLOCK);
        optimal = new ReplacementAlgo(capacity, AlgoType.OPTIMAL);
    }

    public void OnPageAccess(string page)
    {
        //Feeds the same page access request into all algorithms
        fifo.Access(page);
        lru.Access(page);
        clock.Access(page);
        optimal.Access(page);

        server.Broadcast(new
        {
            //Sends the stats of all algorithms to the server
            page,
            fifo = fifo.GetStats(),
            lru = lru.GetStats(),
            clock = clock.GetStats(),
            optimal = optimal.GetStats()
        });
    }
}
//Represents which page replacement algorithm is used
public enum AlgoType { FIFO, LRU, CLOCK, OPTIMAL }
//It is used to create a set of named constants(enum)
public class ReplacementAlgo
{
    //Stores capacity of cache and which algorithm is being used
    private readonly int capacity;
    private readonly AlgoType type;

    private int hits = 0, misses = 0;
    //Stores pages in an order
    private readonly LinkedList<string> list = new();
    //O(1) means constant time → extremely fast
    private readonly HashSet<string> set = new();
    //Used only in Clock algorithm: refBits = reference bit for each page , clockPtr = pointer in circular list
    private readonly Dictionary<string, bool> refBits = new();
    private LinkedListNode<string> clockPtr;
    //Initializes capacity and algorithm type
    public ReplacementAlgo(int capacity, AlgoType type)
    {
        this.capacity = capacity;
        this.type = type;
    }
    //Called when a page is accessed
    public void Access(string page)
    {
        if (set.Contains(page))
        {
            hits++;
            //LRU keeps the most recently used page at the front
            if (type == AlgoType.LRU)
            {
                var node = list.Find(page);
                list.Remove(node);
                list.AddFirst(page);
            }
            //Set reference bit to indicate recent use
            else if (type == AlgoType.CLOCK)
            {
                refBits[page] = true;
            }

            return;
        }

        misses++;
        //Select victim depending on algorithm
        if (set.Count >= capacity)
        {
            string victim = type switch
            {
                AlgoType.FIFO => list.Last!.Value,
                AlgoType.LRU => list.Last!.Value,
                AlgoType.CLOCK => FindClockVictim(),
                AlgoType.OPTIMAL => list.Last!.Value,
                _ => list.Last!.Value
            };
            //Remove victim from both list + set
            Remove(victim);
        }
        //Add new page to cache
        Add(page);
    }
    //Add page at the front of the list
    private void Add(string page)
    {
        list.AddFirst(page);
        set.Add(page);
        //Give page a reference bit. Set clock pointer if not set
        if (type == AlgoType.CLOCK)
        {
            refBits[page] = true;
            if (clockPtr == null)
                clockPtr = list.First;
        }
    }
    //Remove page from list, set, and refBits
    private void Remove(string page)
    {
        var node = list.Find(page);
        if (node != null)
            list.Remove(node);

        set.Remove(page);
        refBits.Remove(page);
    }

    private string FindClockVictim()
    {
        while (true)
        {
            //If reference bit = 0 → evict this page
            if (!refBits[clockPtr.Value])
            {
                string victim = clockPtr.Value;
                clockPtr = clockPtr.Next ?? list.First;
                return victim;
            }
            //If reference bit = 1 → set to 0 and move pointer
            refBits[clockPtr.Value] = false;
            clockPtr = clockPtr.Next ?? list.First;
            //Classic Clock algorithm
        }
    }
    //Returns current stats of the algorithm
    public object GetStats()
    {
        return new
        {
            frames = list.ToArray(),
            hits,
            misses,
            faults = misses,
            hitRatio = hits + misses == 0 ? 0 : (double)hits / (hits + misses)
        };
    }
}
