using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;

namespace MetricHttpServer.Data
{
    static class MetricsData
    {
        /// <summary>
        /// Get list of sensor measures in specific date for each metric
        /// </summary>
        /// <param name="date">Date for which to get metrics data</param>
        /// <returns>List of metrics data</returns>
        public static List<object> GetSensorMeasuresForDate(string date)
        {
            var result = new List<object>();

            using (var connection = new SqliteConnection("Data Source = Data/aranet.db"))
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"SELECT 
                        s.sensor_id,
                        s.name as sensor_name,
                        m.metric_name,
                        u.unit_name,
                        sm.min_measure_value,
                        sm.max_measure_value
                        FROM 
                        sensors s
                        INNER JOIN (
                        SELECT
                        m.sensor_id,
                        m.metric_id,
                        MAX(m.rvalue) as max_measure_value,
                        MIN(m.rvalue) as min_measure_value
                        FROM
                        measures m
                        WHERE date(m.rtime) = @measure_date
                        GROUP BY sensor_id, metric_id) sm on s.sensor_id = sm.sensor_id
                        INNER JOIN metrics m on sm.metric_id = m.metric_id
                        INNER JOIN units u on m.unit_id = u.unit_id";

                    command.Parameters.Add(new SqliteParameter
                    {
                        ParameterName = "@measure_date",
                        Value = date
                    });

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            result.Add(new
                            {
                                SensorId = (long)reader["sensor_id"],
                                SensorName = reader["sensor_name"].ToString(),
                                MetricName = reader["metric_name"].ToString(),
                                UnitName = reader["unit_name"].ToString(),
                                MinValue = (double)reader["min_measure_value"],
                                MaxValue = (double)reader["max_measure_value"]
                            });
                        }
                    }

                    return result;
                }
            }
        }

        /// <summary>
        /// Get list of all sensors with last measure value and date
        /// </summary>
        /// <returns>List of measure data</returns>
        public static List<object> GetSensorListWithLastMeasure()
        {
            var result = new List<object>();
            using (var connection = new SqliteConnection("Data Source = Data/aranet.db"))
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"SELECT
                        s.sensor_id,
                        s.serial_code,
                        s.name as sensor_name,
                        datetime(mo.rtime) as measure_date,
                        mo.rvalue as measure_value,
                        me.metric_name,
                        u.unit_name
                        FROM sensors s
                        LEFT JOIN (SELECT
                        m.sensor_id,
                        MAX(m.rtime) as last_measure_date,
                        MAX(m.reading_id) as max_reading_id
                        FROM measures m
                        GROUP BY m.sensor_id) md ON s.sensor_id = md.sensor_id 
                        LEFT JOIN measures mo on md.last_measure_date = mo.rtime
                        LEFT JOIN metrics me on mo.metric_id = me.metric_id
                        LEFT JOIN units u on me.unit_id = u.unit_id";

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item = new
                            {
                                SensorId = (long)reader["sensor_id"],
                                SerialCode = reader["serial_code"].ToString(),
                                Name = reader["sensor_name"].ToString(),
                                LastMeasureDate = reader["measure_date"] == DBNull.Value ? null : reader["measure_date"].ToString(),
                                LastMeasureValue = reader["measure_value"] as double?,
                                MetricName = reader["metric_name"] == DBNull.Value ? null : reader["metric_name"].ToString(),
                                UnitName = reader["unit_name"] == DBNull.Value ? null : reader["unit_name"].ToString()
                            };

                            result.Add(item);
                        }
                    }
                }
            }

            return result;
        }
    }
}
