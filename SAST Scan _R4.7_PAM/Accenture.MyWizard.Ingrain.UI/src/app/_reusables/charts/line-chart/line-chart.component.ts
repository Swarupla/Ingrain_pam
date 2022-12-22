import { Component, OnInit, ElementRef, ViewChild, Input, OnChanges } from '@angular/core';
import * as d3 from 'd3';
import { CoreUtilsService } from 'src/app/_services/core-utils.service';
import * as moment from 'moment';
import * as _ from 'lodash';
const _moment = moment;

@Component({
  selector: 'app-line-chart',
  templateUrl: './line-chart.component.html',
  styleUrls: ['./line-chart.component.scss']
})
export class LineChartComponent implements OnChanges {
  // @ViewChild('containerLineChart') containerLineChart: ElementRef; old version
  @ViewChild('containerLineChart', { static: true }) containerLineChart: ElementRef;
  @Input() lineChartData: Array<any>;
  @Input() lineChartHeight: number;
  @Input() lineChartWidth: number;
  @Input() frequencyType: any;
  @Input() isSingleLine: any;
  @Input() pageInfo: any;
  @Input() isVisualization: boolean;
  @Input() isRegression: boolean;
  @Input() modelMonitoring: boolean;
  // @Input() visualizationResponse.xlabelname;
  // @Input() visualizationResponse.ylabelname;
  // @Input() legend;
  // @Input() target;
  @Input() visualizationResponse;
  target: string;

  isSingleLineChart: boolean;
  margin: any = { top: 20, bottom: 20, left: 30, right: 20 };
  dataCopy;
  pageNumber: number = 1;
  maxLimit: number = 15;
  fullTextTooltip: any;
  constructor(private coreUtilsService: CoreUtilsService) { }

  ngOnChanges() {
    (function (arr) {
      arr.forEach(function (item) {
        if (item.hasOwnProperty('remove')) {
          return;
        }
        Object.defineProperty(item, 'remove', {
          configurable: true,
          enumerable: true,
          writable: true,
          value: function remove() {
            this.parentNode.removeChild(this);
          }
        });
      });
    })([Element.prototype, CharacterData.prototype, DocumentType.prototype]);
    this.lineChartHeight = this.isVisualization ? this.lineChartHeight : 350;
    this.lineChartWidth = this.isVisualization ? this.lineChartWidth : window.innerWidth;
    this.isSingleLineChart = (!this.coreUtilsService.isNil(this.isSingleLine) && this.isSingleLine === true);
    if (this.isVisualization) {
      this.target = this.visualizationResponse.target ? this.visualizationResponse.target : this.visualizationResponse.Target
    }
    if (this.isRegression) {
      this.createRegressionLineChart()
    } else {
      this.createLineChart();
    }

  }
  createLineChart() {
    // set the dimensions and margins of the graph
    // this.setHeight();
    const widthChart = this.lineChartWidth > 300 ? this.lineChartWidth / 2 : this.lineChartWidth;
    let width = this.isVisualization ? this.lineChartWidth : (widthChart + 100 - this.margin.left - this.margin.right - 40);
    const height = this.isVisualization ? this.lineChartHeight : (this.lineChartHeight - this.margin.top - this.margin.bottom);
    // parse the RangeTime / time

    const parseTime = d3.timeParse('%d/%m/%Y');
    const parseUTCDate = d3.utcParse('%Y-%m-%dT%H:%M:%S.%LZ');
    const formatUTCDate = d3.timeFormat('%Y-%m-%d');
    const parseDate = d3.timeParse('%Y-%m-%d %H:%M:%S');
    const formatDate = d3.timeFormat('%b-%Y');
    const formatTime = d3.timeFormat('%Y-%m-%d %X');

    const _this = this;
    // format the data
    this.lineChartData.forEach(function (d) {
      const tt = parseUTCDate(d.RangeTime);
      const dd = formatUTCDate(d.RangeTime);
      const ll = formatDate(d.rangeTime);

      if (!_this.coreUtilsService.isNil(d.RangeTime)) {
        const splits = d.RangeTime.split(' ');
        const dateArr = splits[0].split('/');
        const actualDt = dateArr[2] + '-' + dateArr[1] + '-' + dateArr[0] + 'T' + splits[1];

        const ddd = _this.formatDate(actualDt);
        d.RangeTime = ddd._d;
      }

      if (!_this.isSingleLineChart) {
        d.Actual = +d.Actual;
      }
      d.Forecast = +d.Forecast;
    });
    // set the ranges


    // Bug 716247 Ingrain_StageTest_R2.1 - Sprint 2 - Timeseries,Classification - Cosmetic issues with charts and values shown
    // Fix:- increase the width and add overflow scroll in html
    if (this.lineChartData.length > 40) {
      width = width + (this.lineChartData.length * 50);
    }

    const x = d3.scaleTime().range([0, width]);
    const y = d3.scaleLinear().range([height, 0]);

    // append the svg obgect to the body of the page
    const element = this.containerLineChart.nativeElement;
    this.deleteElementIfAlready(element);

    // Define the div for the tooltip
    const div = d3.select(element).append('div')
      .attr('class', 'tooltip')
      .style('opacity', 0);

    const svg = d3.select(element).append('svg')
      .attr('width', width + this.margin.left + this.margin.right + 40)
      .attr('height', height + this.margin.top + this.margin.bottom + 120)
      .append('g')
      .attr('transform',
        'translate(' + this.margin.left + ',' + this.margin.top + ')');
    if (this.isVisualization) {
      svg.attr('transform',
        'translate(' + this.margin.left + ',' + 10 + ')');

    }
    // sort years ascending
    this.lineChartData.sort(function (a, b) {
      return a['RangeTime'] - b['RangeTime'];
    });


    if (this.isVisualization) {
      x.domain(d3.extent(this.lineChartData, function (d) { return d.RangeTime; }));
    } else {
      // Scale the range of the data
      if (this.frequencyType === 'Hourly') {
        const minDateValue = d3.min(this.lineChartData, function (d) { return d.RangeTime; });
        const maxDateValue = d3.max(this.lineChartData, function (d) { return d.RangeTime; });
        x.domain([minDateValue, maxDateValue]);
      } else {
        x.domain(d3.extent(this.lineChartData, function (d) { return d.RangeTime; }));
      }

    }
    let yaxisMinValue;
    let yaxisMaxValue;
    if (!_this.isSingleLineChart) {
      yaxisMinValue = d3.min(this.lineChartData, function (d) { return Math.min(d.Actual, d.Forecast); });
      yaxisMaxValue = d3.max(this.lineChartData, function (d) { return Math.max(d.Actual, d.Forecast); });
    } else {
      yaxisMinValue = d3.min(this.lineChartData, function (d) { return Math.min(d.Forecast); });
      yaxisMaxValue = d3.max(this.lineChartData, function (d) { return Math.max(d.Forecast); });
    }

    const d = this.adjustYAxisGrid(yaxisMinValue, yaxisMaxValue);
    yaxisMinValue = d.min;
    yaxisMaxValue = d.max;

    y.domain([yaxisMinValue, yaxisMaxValue]).nice();


    const color = d3.scaleOrdinal(d3.schemeCategory10);
    color.domain(d3.keys(this.lineChartData[0]).filter(function (key) {
      return key !== 'RangeTime' && key !== '_id';
    }));
    const data = this.lineChartData;
    const mappedData = color.domain().map(function (name) {
      return {
        name: name,
        values: data.map(function (d) {
          return {
            rangeTime: d.RangeTime,
            mapvalue: +d[name]
          };
        })
      };
    });
    // Set the X axis
    svg.append('g')
      .attr('class', 'x axis')
      .attr('transform', 'translate(0,' + height + ')')
      .call(x);

    // Set the Y axis
    svg.append('g')
      .attr('class', 'y axis')
      .call(y)
      .append('text')
      .attr('transform', 'rotate(-90)')
      .attr('y', 6)
      .attr('dy', '.71em')
      .style('text-anchor', 'end');

    const line = d3.line()
    .curve(d3.curveMonotoneX)
      .x(function (d) {
        return x(d.rangeTime);
      })
      .y(function (d) {
        return y(d.mapvalue);
      });


    // Draw the lines
    const svgLineChart = svg.selectAll('.mapped-data')
      .data(mappedData)
      .enter().append('g')
      .attr('class', 'mapped-data');

    svgLineChart.append('path')
      .attr('class', 'line')
      .attr('fill', 'none')
      .attr('d', function (d) {
        return line(d.values);
      })
      .style('stroke', function (d) {
        return d.name === 'Actual' ? '#10ADD3' : '#6DB473';
      });
    // Add the circles
    const mappValueMethod = _this.mapvalue;
    const setLeft = _this.setLeft;
    const setTop = _this.setTop;
    svgLineChart.append('g').selectAll('circle')
      .data(function (d) { return d.values; })
      .enter()
      .append('circle')
      .attr('r', 2)
      .attr('cx', function (dd) { return x(dd.rangeTime); })
      .attr('cy', function (dd) { return y(dd.mapvalue); })
      .attr('fill', function (d) {
        // if (_this.isSingleLineChart) {
        //   return '#ff7f0e';
        // } else {
        //   return (color(this.parentNode.__data__.name) === '#1f77b4')
        //     ? '#ba5ea7' : color(this.parentNode.__data__.name);
        // }
        if (_this.isSingleLineChart) {
          return '#ff7f0e';
        } else {
          // Fixed 716247 - Changed the color code
          return (color(this.parentNode.__data__.name) === '#ff7f0e')
            ? '#6DB473' : color(this.parentNode.__data__.name);
        }
      })
      .attr('stroke', function (d) {
        if (_this.isSingleLineChart) {
          return '#ff7f0e';
        } else {
          return (color(this.parentNode.__data__.name) === '#ff7f0e')
            ? '#6DB473' : color(this.parentNode.__data__.name);
        }
      })
      .on('mouseover', function (d) {
        div.transition()
          .duration(200)
          .style('opacity', .9);
        div.html(mappValueMethod(formatTime(d.rangeTime), d, _this.lineChartData))
          .style('left', _this.setLeft(d3.event, _this.isSingleLineChart, _this.pageInfo))
          .style('top', _this.setTop(d3.event, _this.isSingleLineChart, _this.pageInfo));
      })
      .on('mouseout', function (d) {
        div.transition()
          .duration(500)
          .style('opacity', 0);
      });


    let fomratStr;
    // Bug 733876 Ingrain_StageTest_R2.1:
    // -[Visualization]- View Graphs is showing on hovering at any place in the graph and allignment of values are overlapping
    // Fix:- Format Str is changed
    if (this.isVisualization) {
      fomratStr = '%Y-%b-%d';
    } else {
      if (this.frequencyType === 'Hourly') {
        fomratStr = '%Y-%b-%d %X';
      } else {
        fomratStr = '%Y-%b-%d';
      }
    }
    // Add the X Axis

    const xaxisdraw = svgLineChart.append('g')
      .attr('transform', 'translate(0,' + height + ')');
    if (this.isVisualization) {
      const tickValuesForAxis = data.map(d => d.RangeTime);
      xaxisdraw
        .call(d3.axisBottom(x).tickValues(tickValuesForAxis).tickFormat(d3.timeFormat(fomratStr)));
    } else {
      if (this.frequencyType === 'Hourly') {
        xaxisdraw
          .call(d3.axisBottom(x).ticks(10).tickFormat(d3.timeFormat(fomratStr)));
      } else {
        // added tickValues to remove the duplicate dates
        const tickValuesForAxis = data.map(d => d.RangeTime);
        xaxisdraw
          .call(d3.axisBottom(x).tickValues(tickValuesForAxis).tickFormat(d3.timeFormat(fomratStr)));
      }
    }

    xaxisdraw
      .selectAll('text')
      .style('text-anchor', 'end')
      .attr('transform', 'rotate(-65)')
      .attr('y', 0)
      .attr('font-size', 8)
      .attr('x', -8);

    // text label for the x axis
    if (this.isVisualization) {
      svgLineChart.append('text')
        .attr('transform',
          'translate(' + (width / 2) + ' ,' +
          (height + this.margin.top + 50) + ')')
        .style('text-anchor', 'middle')
        .style('font', '14px sans-serif')
        .text(this.visualizationResponse.xlabelname);
    }

    // end of x axis

    // Add the Y Axis
    svgLineChart.append('g')
      .attr('class', 'y-axis-text')
      .call(d3.axisLeft(y));
    svgLineChart.attr('transform', 'translate(' + this.margin.left + ',' + this.margin.top + ')');

    if (this.isVisualization || this.modelMonitoring) {
      svgLineChart.append('text')
        .attr('transform', 'rotate(-90)')
        .attr('y', 0 - (this.margin.left + 30))
        .attr('x', 0 - (height / 2))
        .attr('dy', '1em')
        .style('text-anchor', 'middle')
        .style('font', '14px sans-serif')
        .text(this.visualizationResponse.ylabelname);
      // .text('Forecast');
      const legend = svgLineChart.append('g')
        .attr('class', 'legendLinear')
        .attr('transform', 'translate(0, ' + (height + 25) + ')');

      legend.selectAll('rect')
        .data(data)
        .enter()
        .append('rect')
        .attr('x', 80)
        .attr('y', -199)
        .attr('width', 12)
        .attr('height', 12)
        .attr('fill', '#6DB473');

      legend.selectAll('text')
        .data(data)
        .enter()
        .append('text')
        .text('Forecast')
        .attr('x', 100)
        .attr('y', -200)
        .attr('text-anchor', 'start')
        .attr('alignment-baseline', 'hanging');
    }

    if (!this.isVisualization) {
      if (this.frequencyType === 'Hourly') {
        const wrapText = _this.wrapWord;
        svgLineChart.selectAll('.tick text')
          .call(wrapText, 80);
      }
    }

    if (this.isVisualization) {
      const wrapText = _this.wrapWordEllipse;
      svgLineChart.selectAll('.y-axis-text text')
        .call(wrapText, 30);
    }
  }
  setHeight() {
    let barHeight = 0;
    let barWidth = 0;
    for (let index = 1; index <= this.lineChartData.length; index++) {
      if (index % 10 === 0) {
        barHeight = barHeight + 5;
        barWidth = this.lineChartData.length > 60 ? barWidth + 150 : barWidth + 15;
        barWidth = this.lineChartData.length > 600 ? barWidth + 160 : barWidth;
      }
    }
    this.lineChartHeight = this.lineChartHeight + barHeight;
    this.lineChartWidth = this.lineChartWidth + barWidth;
  }

  formatDate(date: any) {
    const dateTime = new Date(date);
    date = _moment.utc(dateTime);
    return date ? date : null;
  }
  mapvalue(formattedTime, d: any, data: any) {
    const filterByDate = data.filter(x => x.RangeTime === d.rangeTime);
    let name = '';
    const actualFlilter = filterByDate.filter(value => value.Actual === d.mapvalue);
    const forCastFlilter = filterByDate.filter(value => value.Forecast === d.mapvalue);
    let bckgroundColorClass = '';
    if (actualFlilter.length > 0) {
      name = '<b>Actual : </b>';
      bckgroundColorClass = 'actual-bg-color';
    }
    if (forCastFlilter.length > 0) {
      name = '<b>Forecast : </b>';
      bckgroundColorClass = 'forecast-bg-color';
    }

    const mappedData = d.mapvalue;
    const innerHTML = '<div class=' + bckgroundColorClass + '><div><b>Date : </b>' + formattedTime + '</div><div>' + name
      + ' ' + mappedData + '</div></div>';
    return innerHTML;
  }
  setLeft(event, isSingleLineChart, pageInfo) {
    let left;
    if (!this.isVisualization) {
      if (pageInfo !== undefined && pageInfo != null && pageInfo !== '') {
        if (pageInfo === 'div0') {
          left = (event.pageX) - 630 + 'px';
        } else if (pageInfo === 'div1') {
          left = (event.pageX) - 630 + 'px';
        } else if (pageInfo === 'div2') {
          left = (event.pageX) - 630 + 'px';
        }
      } else {
        if (!isSingleLineChart) {
          left = event.layerX + 'px';
          // left = (event.pageX) - 420 + 'px';
        } else {
          left = event.layerX + 'px';
          // left = (event.pageX) - 630 + 'px';
        }
      }
    } else {
      // Bug 733876 Ingrain_StageTest_R2.1:
      // -[Visualization]- View Graphs is showing on hovering at any place in the graph and allignment of values are overlapping
      // Fix:- Hover added left px
      left = event.layerX + 'px';
    }
    return left;
  }
  setTop(event, isSingleLineChart, pageInfo) {
    let top;
    if (!this.isVisualization) {
      if (pageInfo !== undefined && pageInfo != null && pageInfo !== '') {
        if (pageInfo === 'div0') {
          top = (event.pageY - 28) - 380 + 'px';
        } else if (pageInfo === 'div1') {
          top = (event.pageY - 28) - 980 + 'px';
        } else if (pageInfo === 'div2') {
          top = (event.pageY - 28) - 1580 + 'px';
        }
      } else {
        if (!isSingleLineChart) {
          top = event.layerY + 'px';
          // top = (event.pageY - 28) - 590 + 'px';
        } else {
          top = event.layerY + 'px';
          // top = (event.pageY - 28) - 350 + 'px';
        }
      }
    } else {
      // Bug 733876 Ingrain_StageTest_R2.1:
      // -[Visualization]- View Graphs is showing on hovering at any place in the graph and allignment of values are overlapping
      // Fix:- Hover added top px
      top = event.layerY + 'px';
    }
    return top;
  }
  wrapWord(text, width) {
    text.each(function () {
      // tslint:disable: no-shadowed-variable
      const text = d3.select(this);
      const words = text.text().split(/\s+/).reverse();
      if (words.length > 2) {
        let word;
        let line = [],
          lineNumber = 0;
        const lineHeight = 1.1,
          y = text.attr('y'),
          dy = parseFloat(text.attr('dy'));
        let tspan = text.text(null).append('tspan').attr('x', 0).attr('y', y).attr('dy', dy + 'em');
        while (word = words.pop()) {
          line.push(word);
          tspan.text(line.join(' '));
          if (tspan.node().getComputedTextLength() > width) {
            line.pop();
            tspan.text(line.join(' '));
            line = [word];
            tspan = text.append('tspan').attr('x', 0).attr('y', y).attr('dy', ++lineNumber * lineHeight + dy + 'em').text(word);
          }
        }
      }
    });
  }

  wrapWordEllipse(label, width) {
    label.each(function () {
      let text = d3.select(this);
      text = text._groups[0][0];
      if (text.textContent.length > 4) {
        text.textContent = text.textContent.substring(0, 4) + '...';
      }
    });

  }

  showFullText(d, event, coords) {
    this.fullTextTooltip = d3.select('#xFullTextLine');
    this.fullTextTooltip.transition().duration(100).style('opacity', 1);
    this.fullTextTooltip.html(d);
    this.fullTextTooltip.style('display', 'block');
    this.fullTextTooltip.style('left', coords[0] + 'px');
    this.fullTextTooltip.style('top', coords[1] + 'px');
  }

  hideTooltip(d) {
    this.fullTextTooltip = d3.select('#xFullTextLine');
    this.fullTextTooltip.style('display', 'none');
  }

  deleteElementIfAlready(element: any) {
    if (element.hasChildNodes()) {
      let eleLength = element.childNodes.length;
      do {
        const childNodes = Array.from(element.childNodes);

        Array.prototype.forEach.call(childNodes, function (childNode) {
          childNode.remove();
        });

        eleLength = element.childNodes.length;
      } while (eleLength > 0);
    }
  }

  createRegressionLineChart(noset?) {
    if (noset === undefined) { this.dataCopy = this.lineChartData;}
    
    if ( this.modelMonitoring) {
      const w = window.innerWidth - 200;
     this.lineChartWidth = w - this.margin.left - this.margin.right;
    }

    const widthChart = this.lineChartWidth > 300 ? this.lineChartWidth / 2 : this.lineChartWidth;
    let width = this.isVisualization || this.modelMonitoring ? this.lineChartWidth : (widthChart + 100 - this.margin.left - this.margin.right - 40);
    const height = this.isVisualization ? this.lineChartHeight : (this.lineChartHeight - this.margin.top - this.margin.bottom);

    const _this = this;
    // format the data
    this.lineChartData = this.dataCopy.slice((this.pageNumber - 1)* this.maxLimit,this.pageNumber * this.maxLimit);
    
    this.lineChartData.forEach(function (d) {
      // d.XAxis = +d.XAxis;
      d.YAxis = +d.YAxis;
    });
    // set the ranges
    // const x = d3.scaleLinear().range([0, width]);
    // const y = d3.scaleLinear().range([height, 0]);
    // const x = d3.scaleLinear().rangeRound([0, width]);
    // const x = d3.scaleLinear().rangeRound([0, width]);
    // const x = d3.scaleBand().rangeRound([0, width]);
    const x = d3.scalePoint().range([0, width]);

    const y = d3.scaleLinear().range([height, 0]);

    // append the svg obgect to the body of the page
    const element = this.containerLineChart.nativeElement;
    this.deleteElementIfAlready(element);

    // Define the div for the tooltip
    const div = d3.select(element).append('div')
      .attr('class', 'tooltip')
      .style('opacity', 0);

    const svg = d3.select(element).append('svg')
      .attr('width', width + this.margin.left + this.margin.right + 40)
      .attr('height', height + this.margin.top + this.margin.bottom + 70)
      .append('g')
      .attr('transform',
        'translate(' + (this.margin.left + 10) + ',' + (this.margin.top - 10) + ')');

    const xAxisText = d3.select(element).append('div')
      .attr('id', 'xFullTextLine')
      .style('display', 'none')
      .attr('class', 'tooltip forecast-bg-color')
      .style('font-size', '0.7rem');

    // sort years ascending
    this.lineChartData.sort(function (a, b) {
      return a['XAxis'] - b['XAxis'];
    });

    // x.domain(d3.extent(this.lineChartData, function (d) { return d.XAxis; }));
    // x.domain(this.lineChartData.map((d) => (d.XAxis)));
    x.domain(this.lineChartData.map(function (d) { return d.XAxis; }));
    let yaxisMinValue;
    let yaxisMaxValue;
    yaxisMinValue = d3.min(this.lineChartData, function (d) { return Math.min(d.YAxis); });
    yaxisMaxValue = d3.max(this.lineChartData, function (d) { return Math.max(d.YAxis); });
    const d = this.adjustYAxisGrid(yaxisMinValue, yaxisMaxValue);
    yaxisMinValue = d.min;
    yaxisMaxValue = d.max;

    y.domain([yaxisMinValue, yaxisMaxValue]).nice();


    const color = d3.scaleOrdinal(d3.schemeCategory10);
    color.domain(d3.keys(this.lineChartData[0]).filter(function (key) {
      return key !== 'XAxis' && key !== '_id';
    }));
    const data = this.lineChartData;
    const mappedData = color.domain().map(function (name) {
      return {
        name: name,
        values: data.map(function (d) {
          return {
            xAxis: d.XAxis,
            mapvalue: +d[name]
          };
        })
      };
    });
    // Set the X axis
    svg.append('g')
      .attr('class', 'x axis')
      .attr('transform', 'translate(0,' + height + ')')
      .call(x);

    // Set the Y axis
    svg.append('g')
      .attr('class', 'y axis')
      .call(y)
      .append('text')
      .attr('transform', 'rotate(-90)')
      .attr('y', 6)
      .attr('dy', '.71em')
      .style('text-anchor', 'end');

    const line = d3.line()
    .curve(d3.curveMonotoneX)
      .x(function (d) {
        return x(d.xAxis);
      })
      .y(function (d) {
        return y(d.mapvalue);
      });


    // Draw the lines
    const svgLineChart = svg.selectAll('.mapped-data')
      .data(mappedData)
      .enter().append('g')
      .attr('class', 'mapped-data');

    svgLineChart.append('path')
      .attr('class', 'line')
      .attr('fill', 'none')
      .attr('d', function (d) {
        return line(d.values);
      })
      .style('stroke', function (d) {
        return d.name === 'Actual' ? '#10ADD3' : '#6DB473';
      });
    // Add the circles
    const mappValueMethod = _this.mapRegressionValue;
    const setLeft = _this.setLeft;
    const setTop = _this.setTop;
    svgLineChart.append('g').selectAll('circle')
      .data(function (d) { return d.values; })
      .enter()
      .append('circle')
      .attr('r', 2)
      .attr('cx', function (dd) { return x(dd.xAxis); })
      .attr('cy', function (dd) { return y(dd.mapvalue); })
      .attr('fill', function (d) {
        return '#ff7f0e';
      })
      .attr('stroke', function (d) {
        return '#ff7f0e';
      })
      .on('mouseover', function (d) {
        div.transition()
          .duration(200)
          .style('opacity', .9);
        div.html(mappValueMethod(d, _this.lineChartData))
          .style('left', _this.setLeft(d3.event, _this.isSingleLineChart, _this.pageInfo))
          .style('top', _this.setTop(d3.event, _this.isSingleLineChart, _this.pageInfo));
        // .style('left', (d3.event.pageX - this.width) + 'px')
        // .style('top', (d3.event.pageY - this.height) + 'px');
      })
      .on('mouseout', function (d) {
        div.transition()
          .duration(500)
          .style('opacity', 0);
      });

    // Add the X Axis
    const xaxisdraw = svgLineChart.append('g')
      .attr('transform', 'translate(0,' + height + ')')
      .attr('class', 'x-Axis');
    // if (this.frequencyType === 'Hourly') {
    //   xaxisdraw
    //     .call(d3.axisBottom(x).ticks(10).tickFormat(d3.timeFormat(fomratStr)));
    // } else {
    // added tickValues to remove the duplicate dates
    const tickValuesForAxis = data.map(d => d.XAxis);
    xaxisdraw
      .call(d3.axisBottom(x).tickValues(tickValuesForAxis));

    // }
    xaxisdraw
      .selectAll('text')
      .style('text-anchor', 'end')
      .attr('dx', '-.8em')
      .attr('dy', '.15em')
      .attr('transform', 'rotate(-65)')
      .on('mouseover', (d, index, elements) => {
        if (typeof d == 'number') {
          d = d.toString();
        }

        if (d.length > 4) {
          let event = d3.event;
          let coords = [event.offsetX, event.offsetY + 40];
          this.showFullText(d, event, coords);
        }
      })
      .on('mouseleave', d => {
        if (typeof d == 'number') {
          d = d.toString();
        }

        if (d.length > 4) {
          this.hideTooltip(d);
        }
      });

    const wrapText = _this.wrapWordEllipse;
    svg.selectAll('g.x-Axis text')
      .call(wrapText, 30);

    svgLineChart.append('text')
      .attr('transform',
        'translate(' + (width / 2 - 35) + ' ,' +
        (height + this.margin.top + 42) + ')')
      .style('text-anchor', 'middle')
      .style('font', '12px sans-serif')
      .text(this.visualizationResponse.xlabelname);
    // end of x axis

    // Add the Y Axis
    svgLineChart.append('g')
    .attr('class', 'y-axis-text')
      .call(d3.axisLeft(y));
    svgLineChart.attr('transform', 'translate(' + this.margin.left + ',' + this.margin.top + ')');

    svgLineChart.append('text')
      .attr('transform', 'rotate(-90)')
      .attr('y', 0 - (this.margin.left + 40))
      .attr('x', 0 - (height / 2))
      .attr('dy', '1em')
      .style('text-anchor', 'middle')
      .style('font', '14px sans-serif')
      .text(this.visualizationResponse.ylabelname);

    const legend = svgLineChart.append('g')
      .attr('class', 'legendLinear')
      .attr('transform', 'translate(0, ' + (height + 35) + ')');

    legend.selectAll('rect')
      .data(this.visualizationResponse.legend)
      .enter()
      .append('rect')
      .attr('x', -20)
      .attr('y', 32)
      .attr('width', 12)
      .attr('height', 12)
      .attr('fill', '#6DB473');

    legend.selectAll('text')
      .data(this.visualizationResponse.legend)
      .enter()
      .append('text')
      .text(this.visualizationResponse.legend[0])
      .attr('x', -4)
      .attr('y', 33)
      .attr('text-anchor', 'start')
      .style('font', '12px sans-serif')
      .attr('alignment-baseline', 'hanging');
    // if (this.frequencyType === 'Hourly') {
    //   const wrapText = _this.wrapWord;
    //   svgLineChart.selectAll('.tick text')
    //     .call(wrapText, 80);
    // }

    if (this.isVisualization) {
      const wrapText = _this.wrapWordEllipse;
      svgLineChart.selectAll('.y-axis-text text')
      .call(wrapText, 30)
      .append('title')
      .text(function (d) { return d; });

    }
  }

  mapRegressionValue(d: any, data: any) {
    const filterByXAxis = data.filter(x => x.XAxis === d.xAxis);
    let name = '';
    const yAxisFlilter = filterByXAxis.filter(value => value.YAxis === d.mapvalue);
    let bckgroundColorClass = '';
    if (yAxisFlilter.length > 0) {
      name = '<b> Prediction : </b>';
      bckgroundColorClass = 'forecast-bg-color';
    }

    const mappedData = d.mapvalue;
    const innerHTML = '<div class=' + bckgroundColorClass + '><div>' + '</div><div>' + name
      + ' ' + mappedData + '</div></div>';
    return innerHTML;
  }

  adjustYAxisGrid(yaxisMinValue, yaxisMaxValue) {
    const d = {
      min: yaxisMinValue,
      max: yaxisMaxValue
    };
    if (d.min > 0) {
      d.min = 0;
    } else {
      d.min = d.min * 2;
    }

    d.max = (Math.round(d.max));
    d.max = d.max * (1.5);

    return d;
  }


  nextData(){

    this.lineChartData = this.dataCopy.slice((this.pageNumber - 1)* this.maxLimit,this.pageNumber * this.maxLimit);
   //  this.pageNumber++;
    this.createRegressionLineChart('noset');
   }
 
   changePage($event) {
     this.pageNumber = $event;
     this.nextData();
   }
}
