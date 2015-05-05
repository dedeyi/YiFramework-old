﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Objects;
using System.Data.Objects.DataClasses;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace YiFramework.Core
{
    /// <summary>
    /// 数据访问基类，对数据上下文增删查改最基本的封装
    /// </summary>
    /// <typeparam name="TContext">数据库上下文</typeparam>
    /// <typeparam name="TEntity">数据库表对象</typeparam>
    public class DbRepository<TContext, TEntity> : IRepository<TEntity>
        where TContext : DbContext, new()
        where TEntity : DbSet
    {
        /// <summary>
        /// 数据库上下文
        /// </summary>
        protected readonly TContext Context;

        /// <summary>
        /// 当前数据集对象
        /// </summary>
        protected readonly DbSet<TEntity> Entities;

        public DbRepository()
        {
            Context = ContextManager.Instance<TContext>();
            Entities = this.Context.Set<TEntity>();
        }
        /// <summary>
        /// 保存当前上下文修改,只对当前数据访问对象所在的上下文进行SaveChange
        /// </summary>
        public virtual int SaveChange()
        {
            return Context.SaveChanges();
        }

        #region 查询

        /// <summary>
        /// 根据主键获取一个实体
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public virtual TEntity GetByKey(params object[] id)
        {
            return Entities.Find(id);
       }

        /// <summary>
        /// 根据实体中主键获取实体
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public TEntity GetByKeys(TEntity entity)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 查询单个数据实体,相当于SingleOrDefault方法
        /// </summary>
        /// <param name="whereLamb">查询条件表达式</param>
        /// <returns>返回单个实体对象。不存在会返回null，查找到多个实体会抛出异常</returns>
        public virtual TEntity GetEntity(Expression<Func<TEntity, bool>> whereLamb)
        {
            return Entities.SingleOrDefault(whereLamb);
        }

        /// <summary>
        /// 查询
        /// </summary>
        /// <param name="whereLamb">查询条件表达式</param>
        /// <returns>查询结果集</returns>
        public virtual IQueryable<TEntity> Where(Expression<Func<TEntity, bool>> whereLamb)
        {
            return this.Entities.Where(whereLamb);
        }

        /// <summary>
        /// 获取所有数据集
        /// </summary>
        /// <returns>返回 数据表集合对象</returns>
        public virtual IQueryable<TEntity> GetList()
        {
            return Entities;
        }

        /// <summary>
        /// 获取实体表总记录条数
        /// </summary>
        /// <returns></returns>
        public virtual int GetTotal()
        {
            return Entities.Count();
        }

        /// <summary>
        /// 获取查询条件下的记录条数
        /// <param name="whereLamb">查询条件表达式</param>
        /// </summary>
        /// <returns></returns>
        public virtual int GetTotal(Expression<Func<TEntity, bool>> whereLamb)
        {
            return Entities.Count(whereLamb);
        }

        /// <summary>
        /// 获取分页数据
        /// </summary>
        /// <typeparam name="orderType">排序字段类型</typeparam>
        /// <param name="exp">筛选表达式</param>
        /// <param name="orderName">排序字段</param>
        /// <param name="isASC">是否ASC排序</param>
        /// <param name="pagetion"></param>
        /// <returns>排序结果集</returns>
        public virtual IQueryable<TEntity> GetList<orderType>(Expression<Func<TEntity, bool>> whereLamb, Expression<Func<TEntity, orderType>> orderName, bool isASC, Pagetion pagetion)
        {
            pagetion.total = GetTotal(whereLamb);
            if (isASC)
            {
                return Entities
                    .Where(whereLamb)
                    .OrderBy(orderName)
                    .Skip((pagetion.page - 1) * pagetion.rows)
                    .Take(pagetion.rows)
                    .AsQueryable();
            }
            return Entities
                .Where(whereLamb)
                .OrderByDescending(orderName)
                .Skip((pagetion.page - 1) * pagetion.rows)
                .Take(pagetion.rows)
                .AsQueryable();
        }

        #endregion

        #region 添加

        /// <summary>
        /// 添加记录
        /// <param name="saveChange">是否保存当前上下文状态</param>
        /// </summary>
        /// <param name="entity">如果saveChange 是true,返回保存成功状态，否则返回true</param>
        public virtual bool Add(TEntity entity, bool saveChange)
        {
            if (entity == null) throw new ArgumentNullException();
            Context.Entry<TEntity>(entity).State=EntityState.Added;
            if (saveChange) { return this.SaveChange() > 0; }
            return true;
        }


        #endregion

        #region 编辑

        /// <summary>
        /// 更新 【要修改的对象entity，必须保证存在于上下文之中(通过EF查询出来的而非自己构造的)】
        /// </summary>
        /// <param name="entity">待修改的实体对象</param>
        /// <param name="saveChange">是否保存当前上下文状态</param>
        public virtual bool Update(TEntity entity, bool saveChange)
        {
            Context.Entry(entity).State = EntityState.Modified;
            if (saveChange) { return this.SaveChange() > 0; }
            return true;
        }

        #endregion

        #region 删除
        /// <summary>
        /// 删除 , 此处为直接删除数据库记录【要删除的对象entity，必须保证存在于上下文之中(通过EF查询出来的而非自己构造的)】
        /// </summary>
        /// <param name="entity">待删除的实体对象</param>
        /// <param name="saveChange">是否保存当前上下文状态</param>
        public virtual bool Delete(TEntity entity, bool saveChange)
        {
            if (entity == null) throw new ArgumentNullException();
            Context.Entry(entity).State = EntityState.Deleted;
            if (saveChange) { return this.SaveChange() > 0; }
            return true;
        }

        /// <summary>
        /// 根据条件 批量删除记录
        /// </summary>
        /// <param name="whereLamb"></param>
        /// <param name="saveChange"></param>
        /// <returns></returns>
        public virtual bool Delete(Expression<Func<TEntity, bool>> whereLamb, bool saveChange)
        {
            if (whereLamb == null) throw new ArgumentNullException();
            var items = Entities.Where(whereLamb);
            if (items != null)
            {
                foreach (var item in items)
                {
                    Context.Entry(item).State=EntityState.Deleted;
                }
            }
            if (saveChange) { return this.SaveChange() > 0; }
            return true;

        }

        /// <summary>
        /// 删除多个实体
        /// </summary>
        /// <param name="items"></param>
        /// <param name="saveChange"></param>
        /// <returns></returns>
        public virtual bool Delete(IEnumerable<TEntity> items, bool saveChange)
        {
            if (items != null)
            {
                foreach (var item in items)
                {
                    Context.Entry(item).State = EntityState.Deleted;
                }
            }
            if (saveChange) { return this.SaveChange() > 0; }
            return true;
        }


        #endregion

        #region 执行T-SQL

        public virtual IEnumerable<TEntity> SqlQuery(string commandText, params Object[] para)
        {
            return Context.Database.SqlQuery<TEntity>(commandText,para);
        }

        public virtual int ExecuteSqlCommand(string commandText, params Object[] para)
        {
            return Context.Database.ExecuteSqlCommand(commandText,para);
        }

        #endregion


        /// <summary>
        /// 获取key
        /// </summary>
        /// <param name="entityType"></param>
        /// <returns></returns>
        public virtual string GetKeyProperty(Type entityType)
        {
            foreach (var prop in entityType.GetProperties())
            {
                var attr = prop.GetCustomAttributes(typeof(EdmScalarPropertyAttribute), false).FirstOrDefault()
                    as EdmScalarPropertyAttribute;
                if (attr != null && attr.EntityKeyProperty)
                    return prop.Name;
            }
            return null;
        }

        /// <summary>
        /// 获取多个主键
        /// </summary>
        /// <param name="entityType"></param>
        /// <returns></returns>
        public virtual List<string> GetKeysProperties(Type entityType)
        {
            List<string> keys = new List<string>();
            foreach (var prop in entityType.GetProperties())
            {
                var attr = prop.GetCustomAttributes(typeof(EdmScalarPropertyAttribute), false).FirstOrDefault()
                    as EdmScalarPropertyAttribute;
                if (attr != null && attr.EntityKeyProperty)
                    keys.Add(prop.Name);
            }
            return keys;
        }

    }
}
