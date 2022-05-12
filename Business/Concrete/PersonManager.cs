using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using Business.Abstract;
using Business.Constants;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using Newtonsoft.Json.Linq;

namespace Business.Concrete
{
    public class PersonManager : IPersonService
    {
        private IPersonDal _personDal;

        public PersonManager(IPersonDal personDal)
        {
            _personDal = personDal;
        }

        public IResult Add(Person person)
        {
            try
            {
                _personDal.Add(person);
                return new SuccessResult(Messages.AddSuccess);
            }
            catch (Exception)
            {
                return new ErrorResult(Messages.AddFailure);
            }
        }

        public IResult Delete(Person person)
        {
            try
            {
                _personDal.Delete(person);
                return new SuccessResult(Messages.DeleteSuccess);
            }
            catch (Exception)
            {
                return new ErrorResult(Messages.DeleteFailure);
            }
        }

        public IResult Update(Person person)
        {
            try
            {
                _personDal.Update(person);
                return new SuccessResult(Messages.UpdateSuccess);
            }
            catch (Exception)
            {
                return new ErrorResult(Messages.UpdateFailure);
            }
        }

        public IDataResult<Person> GetById(int personId)
        {
            try
            {
                return new SuccessDataResult<Person>(_personDal.Get(p => p.id == personId));
            }
            catch (Exception)
            {
                return new ErrorDataResult<Person>(_personDal.Get(p => p.id == personId));
            }

        }

        public IDataResult<List<Person>> GetList()
        {
            try
            {
                return new SuccessDataResult<List<Person>>(_personDal.GetList().ToList());
            }
            catch (Exception)
            {
                return new ErrorDataResult<List<Person>>(_personDal.GetList().ToList());
            }
        }

        public IDataResult<List<Person>> GetListByCity(string city)
        {
            try
            {
                return new SuccessDataResult<List<Person>>(_personDal.GetList(p => p.city == city).ToList());
            }
            catch (Exception)
            {
                return new ErrorDataResult<List<Person>>(_personDal.GetList(p => p.city == city).ToList());
            }
        }

        public object Aggregate1()
        {
            JObject data = JObject.Parse(File.ReadAllText(@"C:\Users\User\Documents\GitHub\Net-Core-API-application\WebAPI\AggregateDto1.json"));

            return data;
        }

        public object Aggregate2()
        {
            JObject data = JObject.Parse(File.ReadAllText(@"C:\Users\user\Documents\GitHub\Net-Core-API-application\WebAPI\AggregateDto2.json"));

            return data;
        }
    }
}
