﻿using System.Collections.Generic;
using System.Linq;
using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Domain.Randomizations;

namespace GeneticSharp.Extensions.Sudoku
{
  /// <summary>
  /// This more elaborated chromosome manipulates rows instead of cells, and each of its 9 gene holds an integer for the index of the row's permutation amongst all that respect the target mask.
  /// Permutations are computed once when a new Sudoku is encountered, and stored in a static dictionary for further reference.
  /// </summary>
  public class SudokuPermutationsChromosome : ChromosomeBase, ISudokuChromosome
  {
    protected readonly Sudoku TargetSudoku;
    protected readonly List<List<List<int>>> TargetRowsPermutations;


    public SudokuPermutationsChromosome() : this(null)
    {
    }

    public SudokuPermutationsChromosome(Sudoku targetSudoku) : this(targetSudoku, 9)
    {
      
    }

    public SudokuPermutationsChromosome(Sudoku targetSudoku, int length) : base(length)
    {
      TargetSudoku = targetSudoku;
      TargetRowsPermutations = GetRowsPermutations(TargetSudoku);
      for (int i = 0; i < Length; i++)
      {
        ReplaceGene(i, GenerateGene(i));
      }
    }

    public override Gene GenerateGene(int geneIndex)
    {

      var rnd = RandomizationProvider.Current;
      var permIdx = rnd.GetInt(0, TargetRowsPermutations[geneIndex].Count);
      return new Gene(permIdx);
    }

    public override IChromosome CreateNew()
    {
      var toReturn = new SudokuPermutationsChromosome(TargetSudoku);
      return toReturn;
    }


    public virtual List<Sudoku> GetSudokus()
    {
      var listInt = new List<int>(81);
      for (int i = 0; i < 9; i++)
      {
        int permIDx = GetPermutationIndex(i);
        var perm = TargetRowsPermutations[i][permIDx % TargetRowsPermutations[i].Count].ToList();
        listInt.AddRange(perm);
      }
      var sudoku = new Sudoku(listInt);
      return new List<Sudoku>(new[] { sudoku });
    }


    protected virtual int GetPermutationIndex(int rowIndex)
    {
      return (int) GetGene(rowIndex).Value;
    }



    /// <summary>
    /// This method computes for each row the list of digit permutations that respect the target mask, that is the list of valid rows discarding columns and boxes
    /// </summary>
    /// <param name="sudoku">the target sudoku to account for</param>
    /// <returns>the list of permutations available</returns>
    public List<List<List<int>>> GetRowsPermutations(Sudoku sudoku)
    {
      if (sudoku==null)
      {
        return UnfilteredPermutations;
      }
      if (!_rowsPermutations.TryGetValue(sudoku, out var toReturn))
      {
        lock (_rowsPermutations)
        {
          if (!_rowsPermutations.TryGetValue(sudoku, out toReturn))
          {
            toReturn = new List<List<List<int>>>(9);
            for (int i = 0; i < 9; i++)
            {
              var tempList = new List<List<int>>();
              foreach (var perm in AllPermutations)
              {
                if (!Range9.Any(j => sudoku.GetCell(i, j) > 0 && (perm[j] != sudoku.GetCell(i, j))))
                {
                  tempList.Add(perm);
                }
              }
              toReturn.Add(tempList);
            }
            _rowsPermutations[sudoku] = toReturn;
          }
        }
      }
      return toReturn;
    }

    /// <summary>
    /// Produces 9 copies of the complete list of permutations
    /// </summary>
    public static List<List<List<int>>> UnfilteredPermutations
    {
      get
      {
        if (!_unfilteredPermutations.Any())
        {
          lock (_unfilteredPermutations)
          {
            if (!_unfilteredPermutations.Any())
            {
              _unfilteredPermutations = Range9.Select(i => AllPermutations).ToList();
            }
          }
        }
        return _unfilteredPermutations;
      }
    }

    /// <summary>
    /// Builds the complete list permutations for {1,2,3,4,5,6,7,8,9}
    /// </summary>
    public static List<List<int>> AllPermutations
    {
      get
      {
        if (!_allPermutations.Any())
        {
          lock (_allPermutations)
          {
            if (!_allPermutations.Any())
            {
              _allPermutations = GetPermutations(Enumerable.Range(1, 9), 9);
            }
          }
        }
        return _allPermutations;
      }
    }

    private static readonly Dictionary<Sudoku, List<List<List<int>>>> _rowsPermutations = new Dictionary<Sudoku, List<List<List<int>>>>();

    private static readonly List<int> Range9 = Enumerable.Range(0, 9).ToList();

    private static List<List<int>> _allPermutations = new List<List<int>>();
    private static List<List<List<int>>> _unfilteredPermutations = new List<List<List<int>>>();

    static List<List<T>> GetPermutations<T>(IEnumerable<T> list, int length)
    {
      if (length == 1) return list.Select(t => new T[] { t }.ToList()).ToList();

      return GetPermutations(list, length - 1)
        .SelectMany(t => list.Where(e => !t.Contains(e)),
          (t1, t2) => t1.Concat(new T[] { t2 }).ToList()).ToList();
    }



  }
}