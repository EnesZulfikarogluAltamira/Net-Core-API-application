using Core.Utilities.Results;
using Entities.Concrete;
using Entities.Dtos;
using System;
using System.Collections.Generic;
using System.Text;

namespace Business.Abstract
{
    public interface IPersonService
    {
        IDataResult<Person> GetById(int id);
        IDataResult<List<Person>> GetList();
        IDataResult<List<Person>> GetListByCity(string city);
        IResult Add(Person person);
        IResult Delete(Person person);
        IResult Update(Person person);
        object Aggregate1(AggregatorDto myDto);
        object Aggregate2(AggregatorDto1 myDto);
        object Aggregate3();
    }
}
