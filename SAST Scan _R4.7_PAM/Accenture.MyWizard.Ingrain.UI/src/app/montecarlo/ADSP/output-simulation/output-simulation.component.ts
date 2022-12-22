import { Component, OnInit, ViewChild, ElementRef, Input } from '@angular/core';
import { FormBuilder, FormGroup, FormArray, AbstractControl, FormControl } from '@angular/forms';
// import { BsModalService, BsModalRef } from 'ngx-bootstrap/modal';
import { ApiCore } from '../../services/api-core.service';
import { ApiService } from 'src/app/_services/api.service';
import * as d3 from 'd3';
import { AlertService } from '../../services/alert-service.service';
import d3Tip from 'd3-tip';
import { SimulationHeader } from '../../shared/component/simulation-header/simulation-header.service';
import { SimulationValidatorsService } from '../../services/simulation-validators.service';
import * as _ from 'lodash';
import { LoaderState } from '../../services/loader-state.service';

@Component({
  selector: 'app-output-simulation',
  templateUrl: './output-simulation.component.html',
  styleUrls: ['./output-simulation.component.scss']
})
export class OutputSimulationComponent implements OnInit {

  saveOutputDataRequest;

  horizontalChart = 'Effort';
  templateDataForm: FormGroup;
  simulatedDataToggle = false;
  certainity: any;
  templateData: any;
  templateId = '';

  xAxisEfforts; yAxisEfforts;

  /* dancing chart variables : start */
  @ViewChild('containerHdualChart', { static: true }) containerHdualChart: ElementRef;
  color = 'green';
  w = 1200;   // 1150;// 1260; 
  h = 320;// 220; // 300;
  margin = { top: 20, right: 30, bottom: 30, left: 50 }; // left: 30
  width = this.w - this.margin.left - this.margin.right;
  height = this.h - this.margin.top - this.margin.bottom;
  x0; y0; y1; max; min; svg; element; data;
  xAxis; y0Axis; y1Axis; yMax; yMin;

  // calculations for plotting ideal/normal distribution curve
  numBuckets = 20; // ticks(bins) in histogram
  numberOfDataPoints = 100;
  mean = 20;
  stdDeviation = 5;

  probability; variance; idealData; actualData; bins; linePlot; lineTooltipDiv;
  /* dancing chart variables : end */

  certainityValue: number; defectValue: number; teamSizeValue: number; effortValue: number; scheduleValue: number;
  selectedTemplateVersion; selectedSimulationVersion; changeFieldName; simulationResult; simulationTableData;
  certainityOldValue: number; defectOldValue: number; teamSizeOldValue: number; effortOldValue: number; scheduleOldValue: number;
  effortSimulatedTableData = []; teamSizeSimulatedTableData = []; scheduleSimulatedTableData = []; simulatedTableMenu = 'Effort (Hrs)';
  defectHistogramArray = []; targetCertanityValue = 0;
  targetColumnName;
  // certainityPreviousValue: number; defectPreviousValue: number; teamSizePreviousValue: number; effortPreviousValue: number;
  // schedulePreviousValue: number;
  observationText = '';

  // New parameter

  whatIfDefectVal: number; whatIfTeamSizeVal: number; whatIfEffortVal: number; whatIfScheduleVal: number;
  currentReleaseName: string; defectIncrementalFlag: number; effortIncrementalFlag: number; teamSizeIncrementalFlag: number;
  scheduleIncrementalFlag: number;
  ifTeamSizeCaptured;
  defectPercentChange: number; effortPercentChange: number; teamSizePercentChange: number; schedulePercentChange: number;

  dataForVerticalBar;
  dataForHorizontalBar;
  // runSimulationData;
  ifTeamSizeEnabled: string;

  constructor(private ingrainApiService: ApiService,
    public apiCore: ApiCore, private _simulationHeader: SimulationHeader,   // private _modalService: BsModalService, 
    private _alertService: AlertService,
    private simulationValidatorsService: SimulationValidatorsService, private loader: LoaderState) {
    this.templateDataForm = new FormGroup({
      certaintySlider: new FormControl(0),
      defects: new FormControl(0),
      efforts: new FormControl(0),
      teamSize: new FormControl(0),
      schedule: new FormControl(0)
    });
  }

  ngOnInit() { }

  populateTemplateData(data) {
    const formControl = this.templateDataForm.controls;

    formControl.defects.setValue(data.PercentChange['Defect']);
    // this.defectOldValue = data['TargetVariable'];
    this.targetColumnName = data['TargetColumn'];
    this.defectPercentChange = data.PercentChange['Defect'];

    formControl.efforts.setValue(data.PercentChange['Effort (Hrs)']);  // (data.Influencers['Effort (Hrs)']);
    // this.effortOldValue = data.Influencers['Effort (Hrs)'];
    this.effortPercentChange = data.PercentChange['Effort (Hrs)'];

    formControl.teamSize.setValue(data.PercentChange['Team Size']);
    // this.teamSizeOldValue = data.Influencers['Team Size'];
    this.teamSizePercentChange = data.PercentChange['Team Size'];
    if (this.ifTeamSizeEnabled === 'False') {
      formControl['teamSize'].disable();
      this.ifTeamSizeCaptured = 0;
    }
    //  else {
    //   formControl['teamSize'].enable();
    // }

    formControl.schedule.setValue(data.PercentChange['Schedule (Days)']);
    //  this.scheduleOldValue = data.Influencers['Schedule (Days)'];
    this.schedulePercentChange = data.PercentChange['Schedule (Days)'];

    formControl.certaintySlider.setValue(data['TargetCertainty']);
    this.certainityOldValue = data['TargetCertainty'];

    /* new code to show the values on label for influencers : start */
    this.whatIfDefectVal = data['TargetVariable'];
    this.whatIfEffortVal = data.Influencers['Effort (Hrs)'];
    this.whatIfScheduleVal = data.Influencers['Schedule (Days)'];
    this.whatIfTeamSizeVal = data.Influencers['Team Size'];

    this.defectIncrementalFlag = data.IncrementFlags['Defect'];
    this.effortIncrementalFlag = data.IncrementFlags['Effort (Hrs)'];
    this.teamSizeIncrementalFlag = data.IncrementFlags['Team Size'];
    this.scheduleIncrementalFlag = data.IncrementFlags['Schedule (Days)'];
    /* new code : end */
  }

  simulatedDataToggling() {
    this.simulatedDataToggle = !this.simulatedDataToggle;
  }

  refreshData(influencerName) {
    const formControl = this.templateDataForm.controls;
    // formControl.defects.setValue(this.defectOldValue);
    // formControl.efforts.setValue(this.effortOldValue);
    // formControl.teamSize.setValue(this.teamSizeOldValue);
    // formControl.schedule.setValue(this.scheduleOldValue);
    // formControl.certaintySlider.setValue(this.certainityOldValue);
    if (influencerName === 'defect') {
      formControl['defects'].enable();
    } else if (influencerName === 'effort') {
      formControl['efforts'].enable();
    } else if (influencerName === 'teamSize') {
      if (this.ifTeamSizeEnabled === 'False') {
        formControl['teamSize'].disable();
        this.ifTeamSizeCaptured = 0;
      }
      //  else {
      //   formControl['teamSize'].enable();
      // }
    } else if (influencerName === 'schedule') {
      formControl['schedule'].enable();
    }
  }

  /**
   * capture the influencer slider chagne vlaue based on influencer selection
   * @param value
   * @param influencerName
   */
  changedValueForInfluencerSlider(value, influencerName) {
    if (influencerName === 'defect') {
      this.defectValue = value;
      this.changeFieldName = this.targetColumnName;
    } else if (influencerName === 'effort') {
      this.effortValue = value;
      this.changeFieldName = 'Effort (Hrs)';
    } else if (influencerName === 'teamSize') {
      this.teamSizeValue = value;
      this.changeFieldName = 'Team Size';
    } else if (influencerName === 'schedule') {
      this.scheduleValue = value;
      this.changeFieldName = 'Schedule (Days)';
    }
    this.postChangedTargetInfluencerVal();
  }


  changedValueForCertaintySlider(value) {
    this._simulationHeader.disableSaveAs = true;
    this.certainity = value * 1;
    this.certainityValue = this.certainity;
    this.changeFieldName = 'TargetCertainty';
    this.postChangedTargetInfluencerVal();
  }

  getStyles(value, minValue, maxValue) {
    const diff = maxValue - minValue;
    const number = value * 1;
    const rangefromMinValue = number - minValue;

    let left = 13;
    left = ((rangefromMinValue / diff) * 100);
    if (left > 88) {
      left = left - 5;
    }
    if (number === maxValue) {
      left = 95;
    }

    const styles = {
      'position': 'relative',
      'left': left + '%',
      'z-index': '1',
      'top': '3px',
      'color': '#10ADD3',
      'font-size': '0.7rem',
      'font-weight': '700'
    };
    return styles;
  }

  getSliderStyles(value, minValue, maxValue, type) {
    const diff = maxValue - minValue;
    const number = value * 1;
    const rangefromMinValue = number - minValue;

    let left = 13;
    left = ((rangefromMinValue / diff) * 100);
    if (left > 88) {
      left = left - 10;
    }
    if (number === maxValue) {
      left = 88;
    }

    const styles = {
      'position': 'relative',
      'left': left + '%',
      'z-index': '0',
      'top': '3px',
      'color': '#10ADD3',
      'font-size': '0.7rem',
      'font-weight': '700'
    };
    return styles;
  }

  /* dancing chart variables : end */

  defectChartDataFormation(mean, standard_deviation, histo) {
    this.element = this.containerHdualChart.nativeElement;
    this.deleteElementIfAlready(this.element);

    const xValueForActual = histo.map(function (d) { return d.XAxisValues; });

    this.numberOfDataPoints = 100;

    const normalDistributionFunction = d3.randomNormal(mean, standard_deviation);
    this.actualData = d3.range(this.numberOfDataPoints).map(normalDistributionFunction);
    // console.log(this.actualData);

    this.probability = 1 / this.numberOfDataPoints;
    this.variance = Math.pow(standard_deviation, 2);


    this.idealData = this.getProbabilityData(xValueForActual, mean, this.variance);
    // console.log('data--', this.idealData)

    this.initSvg();
    this.initTooltipElement();
    // this.normalizedAxis();
    this.drawAxisCharts(histo);
    this.addLegends();
  }

  initTooltipElement() {
    // Add a tooltip div. Here I define the general feature of the tooltip: stuff that do not depend on the data point.
    // Its opacity is set to 0: we don't see it by default.
    this.lineTooltipDiv = d3.select(this.element).append('div')
      .attr('id', 'LineTooltip')
      .style('opacity', 0)
      .attr('class', 'tooltip')
      .style('background-color', 'rgba(0, 0, 0, 0.8)')
      .style('color', 'white')
      .style('border-radius', '2px')
      .style('padding', '10px');
  }

  getProbabilityData(normalizedData, m, v) {
    const data = [];
    // probabily - quantile pairs
    for (let i = 0; i < normalizedData.length; i += 1) {
      const q = normalizedData[i],
        // p = (this.logprobabilityDensityCalculation(q, m, v) * 100),
        p = (this.probabilityDensityCalculation(q, m, v) * 100),
        el = {
          'q': q,
          'p': p
        };
      data.push(el);
    }
    data.sort(function (x, y) { return x.q - y.q; });
    return data;
  }

  // The probability density of the normal distribution
  probabilityDensityCalculation(x, mean, variance) {
    const m = Math.sqrt(2 * Math.PI * variance);
    const e = Math.exp(-Math.pow(x - mean, 2) / (2 * variance));
    // Math.log();
    return e / m;
  }

  logprobabilityDensityCalculation(x, mean, variance) {
    const m = x * variance * (Math.sqrt(2 * Math.PI));
    const e = Math.exp(-Math.pow((Math.log10(x) - mean), 2) / (2 * variance));
    // Math.log();
    return e / m;
  }


  initSvg() {
    this.svg = d3.select(this.element).append('svg')
      // .attr('width', this.width + this.margin.left + this.margin.right)
      // .attr('height', this.height + this.margin.top + this.margin.bottom)
      // 'preserveAspectRatio' and 'viewBox' properties handle the responsiveness of the charts and svgs.
      .attr('preserveAspectRatio', 'xMinYMin meet')
      .attr(
        'viewBox',
        '0 0 ' +
        (this.width + this.margin.left + this.margin.right) +
        ' ' +
        (this.height + this.margin.top + this.margin.bottom)
      )
      .append('g')
      .attr('transform', `translate(${this.margin.left}, ${this.margin.top})`);
  }

  drawAxisCharts(data) {
    const width = this.width - this.margin.left - 10; // this.margin.right;
    const height = this.height - this.margin.top - this.margin.bottom;
    const maxValueofRange = this.whatIfDefectVal; // this.findTheConfidenceInterval(data);

    // X axis
    const x = d3.scaleBand()
      .range([0, width])
      .domain(data.map(function (d) { return d.XAxisValues; }))
      .padding(0.2);
    this.svg.append('g')
      .attr('transform', 'translate(0,' + height + ')')
      .style('color', '#267DB3')
      .call(d3.axisBottom(x))
      .selectAll('text')
      .attr('transform', 'translate(-10,0)rotate(-35)')
      .style('text-anchor', 'end')
      .style('font-size', '0.9rem');

    // Add Y axis
    const y = d3.scaleLinear()
      .domain([d3.min(data, function (d) {
        return d.YAxisValues;
      }), d3.max(data, function (d) {
        return d.YAxisValues;
      })])
      // .domain([0, data.map(function (d) { return d.YAxisValues; })])
      .range([height, 0]);

    this.svg.append('g')
      .call(d3.axisLeft(y))
      .style('font-size', '1rem');

    // Add y1 axis
    const y1Max = d3.max(this.idealData, function (d) { return d.p; });

    const y1 = d3.scaleLinear()
      .domain([0, y1Max])
      .range([height, 0]);

    // this.svg.append('g')
    //   .attr('transform', 'translate(' + (width) + ' ,0)')
    //   .call(d3.axisRight(y1));

    // Add x1 axis
    const x1 = d3.scaleBand()
      .range([0, width])
      .domain(this.idealData.map(function (d) { return d.q; }))
      .padding(0.2);
    this.svg.append('g')
      .attr('transform', 'translate(0,' + height + ')')
      .attr('id', 'x1-axis')
      .style('color', '#267DB3')
      .style('opacity', 0)
      .call(d3.axisBottom(x1))
      .selectAll('text')
      .attr('transform', 'translate(-10,0)rotate(-45)')
      .style('text-anchor', 'end');

    // custom invert function
    x1.invert = (function () {
      const domain = x1.domain();
      const range = x1.range();
      const scale = d3.scaleQuantize().domain(range).range(domain);

      return function (x) {
        return scale(x);
      }
    })()

    const bisect = d3.bisector(function (d) { return d.q; }).left;

    this.svg.append('g')
      .attr('transform', 'translate(' + (width) + ' ,0)')
      .attr('id', 'y1-axis')
      .call(d3.axisRight(y1))
      .style('font-size', '1rem')
      .append('text')
      .attr('class', 'y label')
      .attr('transform', 'rotate(-270)')
      .attr('y', 0 - 60)
      .attr('x', (height / 2))
      .attr('dy', '0.2em')
      .attr('text-anchor', 'middle')
      .text('Probability %')
      .style('fill', '#000')
      .style('font-size', '1.4rem')
      .style('padding-left', '1rem');

    const formatPercent = d3.format('.0%');
    const tip = d3Tip();

    tip
      .attr('class', 'd3-tip')
      .html(d => {
        // console.log(d)
        return (
          `<span style='font-size:11px'>Frequency:</span> <span style='color:red;font-size:11px'>` + d + '</span>'
        );
      });

    this.svg.call(tip);

    const rx = 12;
    const ry = 12;

    // Bars
    this.svg.selectAll('.bar')
      .data(data)
      .enter()
      /* .append('rect')
      .attr('x', function (d) { return x(d.XAxisValues); })
      .attr('width', x.bandwidth()) */
      // .attr('y', function (d) { return y(d.YAxisValues); })
      // .attr('height', function (d) { return height - y(d.YAxisValues); })

      // no bar at the beginning thus:
      // .attr('height', function (d) { return height - y(0); }) // always equal to 0
      //  .attr('height', 0) // always equal to 0
      // .attr('y', function (d) { return y(0); })

      // .on('mouseover', d => tip.show(d.YAxisValues, this))
      .append('path')
      .attr('d', d => `
        M${x(d.XAxisValues)},${height}
        v-3
        a${rx},${ry} 0 0 1 ${rx},${-ry}
        h${x.bandwidth() - 3 * rx}
        a${rx},${ry} 0 0 1 ${rx},${ry}
        v3Z
      `)
      .on('mouseover', (d, i, n) => {
        tip.show(d.YAxisValues, n[i]);
      })
      .on('mouseout', d => tip.hide(d.YAxisValues, this))
      .attr('fill', function (d) {
        if (d.XAxisValues <= maxValueofRange) {
          return 'green';
        } else { return '#990000'; }
      })
      .transition()
      .duration(600)
      .transition()
      // .ease(d3.easeBounce)
      .delay(function (d, i) {
        return i * 50;
      })
      .attr('y', function (d) { return y(d.YAxisValues); })
      .attr('height', function (d) { return height - y(d.YAxisValues); })
      .attr('d', d => `
        M${x(d.XAxisValues)},${height}
        v${-((height - y(d.YAxisValues) - rx))}
        a${rx},${ry} 0 0 1 ${rx},${-ry}
        h${x.bandwidth() - 3 * rx}
        a${rx},${ry} 0 0 1 ${rx},${ry}
        v${(height - y(d.YAxisValues) - rx)}Z
      `)
      .delay(function (d, i) { return (i * 100); });


    // Add axis labels
    this.svg.append('text')
      .attr('class', 'y label')
      .attr('transform', 'rotate(-90)')
      .attr('y', 0 - ( this.margin.left + 10))
      .attr('x', 0 - (height / 2))
      .attr('dy', '1.2em')
      .attr('text-anchor', 'middle')
      .style('font-size', '1.4rem')
      .text('Frequency');

    this.svg.append('text')
      .attr('class', 'x label')
      // .attr('transform', 'rotate(-90)')
      .attr('y', height + this.margin.top + 10)
      .attr('x', (width / 2 + 40))
      .attr('dy', '1.2em')
      .attr('text-anchor', 'middle')
      .style('font-size', '1.4rem')
      .text('Defects closed');

    this.normalizedAxis();

    // drawLine :start

    // draw ideal normal distribution curve
    const lines = this.svg.selectAll('.series')
      .data([1]) // only plot a single line
      .enter().append('g');

    // Circle Hover
    lines.append('circle')
      .attr('r', 5)
      .attr('fill', '#6F257F')
      .attr('stroke', '#6F257F')
      .style('display', 'none');

    // Add the Ideal lines
    lines.append('path')
      .datum(this.idealData)
      .attr('class', 'line')
      .attr('d', this.linePlot)
      .style('fill', 'none')
      .style('stroke', 'black')// function () {return color(series[1]);})
      .style('stroke-width', '2px')
      .style('fill', 'none')
      .on('mouseover', () => {
        const coords = d3.mouse(d3.event.currentTarget);

        lines.select('circle').style('display', null);
        lines.select('circle').attr('cx', coords[0]);
        lines.select('circle').attr('cy', coords[1]);

        const x0 = x1.invert(coords[0]),
          i = bisect(this.idealData, x0, 1),
          d0 = this.idealData[i - 1],
          d1 = this.idealData[i],
          d = x0 - d0.p > d1.p - x0 ? d1 : d0;

        // console.log('Invert Value ' + d.p);
        this.showTooltip((d.p).toFixed(4), coords);
      })
      .on('mouseout', () => {
        lines.select('circle').style('display', 'none');
        this.hideTooltip();
      });

    // this.svg.append('text')
    //   .attr('class', 'x label')
    //   .attr('transform', 'translate(' + (width / 2) + ' ,' + (height + this.margin.bottom + 20) + ')')
    //   // .attr('dy', '1em')
    //   .attr('text-anchor', 'middle')
    //   .attr('dy', '0.6em')
    //   .style('font-size', '0.7rem')
    //   // .text('Fit' + '&nbsp;' + ' Predicted')
    //   .text(`<span style='font-size:11px'><span style='border:2px solid black;'></span> Fit</span> <span style='color:red; font-size:11px;'>Predicted</span>`);
    // drawLine :end
  }

  normalizedAxis() {
    const width = this.width - this.margin.left - this.margin.right;
    const height = this.height - this.margin.top - this.margin.bottom;

    // normalized X Axis scaler function
    const xNormal = d3.scaleLinear()
      .range([0, width])
      .domain(d3.extent(this.idealData, function (d) { return d.q; }));
    // normalized Y Axis scaler function
    const yNormal = d3.scaleLinear()
      .range([height, 0])
      .domain(d3.extent(this.idealData, function (d) { return d.p; }));
    // line plot function
    this.linePlot = d3.line()
      .curve(d3.curveCardinal)
      .x(function (d) { return xNormal(d.q); })
      .y(function (d) { return yNormal(d.p); });
  }

  // function that change this tooltip when the user hover a point.
  // Its opacity is set to 1: we can now see it. Plus it set the text and position of tooltip depending on the datapoint (d)
  showTooltip(dp, coords) {
    const tooltipDiv = d3.select('#LineTooltip');
    tooltipDiv.transition().duration(100).style('opacity', 1);
    tooltipDiv.html(`<span style='font-size:11px'>Probability:</span> <span style='color:red; font-size:11px;'>` + dp + `</span>`);
    tooltipDiv.style('left', coords[0] + 'px');
    tooltipDiv.style('top', coords[1] + 90 + 'px');
  }

  moveTooltip(d) {
    const tooltipDiv = d3.select('#LineTooltip');
    tooltipDiv
      // // .style('left', (d3.mouse(this)[0] + 20) + 'px')
      .style('left', (d3.event.pageX - 34) + 'px')
      .style('top', (d3.mouse(this)[1]) + 'px');
    // .style('top', (d3.event.pageY - 30) + 'px').style('left', (d3.event.pageX + 30) + 'px');
  }
  // function that change this tooltip when the leaves a point: just need to set opacity to 0 again
  hideTooltip() {
    const tooltipDiv = d3.select('#LineTooltip');
    tooltipDiv.transition().duration(100)//.style('visibility', 'hidden');
      .style('opacity', 0);
  }

  addLegends() {

    const legend = this.svg.selectAll('rect')
      .data(['1'])
      .enter().append('g')
      .attr('class', 'legend');

    // draw legend colored rectangles
    legend.append('rect')
      .attr('x', ((this.width / 2) - 60)) //this.width - 18)
      .attr('width', 18)
      .attr('height', 3)
      .attr('y', this.height + this.margin.top)
      .attr('dy', '1em')
      .style('fill', 'black');

    // draw legend text
    legend.append('text')
      .style('font-size', '12px')
      .style('color', '#515151')
      .attr('x', ((this.width / 2) - 30)) //this.width - 24)
      .attr('y', this.height)
      .attr('dy', '2em')
      .style('text-anchor', 'middle')
      .text('Fit');

    legend.append('rect')
      .attr('x', ((this.width / 2) - 5)) //this.width - 18)
      .attr('width', 20)
      .attr('height', 1)
      .attr('dy', '1.5em')
      .attr('y', this.height + this.margin.top)
      .style('fill', '#267DB3');

    // draw legend text
    legend.append('text')
      .style('font-size', '12px')
      .style('color', '#515151')
      .attr('x', ((this.width / 2) + 50)) //this.width - 24)
      .attr('y', this.height)
      .attr('dy', '2em')
      .style('text-anchor', 'middle')
      .text('Predicted');
  }

  /* dancing chart variables : end */


  getRunSimulationResultSet(simulationData) {
    //  simulationData = payload;

    if (simulationData) {
      // console.log(simulationData);
      if (  this.apiCore.paramData.viewSimulationFlag === 'No' && simulationData.FlagTeamSize === 0 &&
      simulationData.TeamSize ) {
       // this._alertService.warning('Only the overall team size would be considered as most of the data in phases is not captured. Also, the sensitivity would not be available for team size');
      }
      this.observationText = null;
      if (simulationData.hasOwnProperty('Observation')) { this.observationText = simulationData.Observation; }
      this.simulationResult = simulationData;

      this.dataForVerticalBar = simulationData.Defect.Sensitivity_Analysis;
      this.dataForHorizontalBar = simulationData.Effort.Sensitivity_Analysis;

      if (simulationData.Effort != null) {
        this.currentReleaseName = simulationData.SelectedCurrentRelease;

        this.selectedSimulationVersion = simulationData.SimulationID;
        this.selectedTemplateVersion = simulationData.TemplateID;
        this.ifTeamSizeEnabled = this.apiCore.paramData.TeamCheckedBox;
        this.ifTeamSizeCaptured = simulationData.FlagTeamSize;

        this.populateTemplateData(this.simulationResult);
        this.targetCertanityValue = simulationData.TargetCertainty;

        this.stdDeviation = simulationData.Defect.standard_deviation;
        this.mean = simulationData.Defect.mean;
        this.defectHistogramArray = simulationData.Defect.histogram;
        this.defectChartDataFormation(this.mean, this.stdDeviation, this.defectHistogramArray);

        this.simulationTableData = simulationData.SensitivityReport;

        const effortsSimulation = Object.entries(simulationData.SensitivityReport);
        this.effortSimulatedTableData = this.generateSimulatedTableData(effortsSimulation);

        this.setOutputData();
        // this.setVluesForRefresh(this.simulationResult);
        this.observationText = simulationData.Observation;
      }
    }
  }

  // getSensitivityAnalysisFeatureName(sensitivityObj) {
  //   const sensitivityFeature = _(sensitivityObj).toPairs()
  //     .orderBy([1], ['desc'])
  //     .fromPairs()
  //     .value();

  //   const sortedFeatures = Object.keys(sensitivityFeature).join(', ');
  //   return sortedFeatures;
  // }


  generateSimulatedTableData(simulatedData) {
    const finalArrary = [];
    for (const i in simulatedData) {
      if (simulatedData) {
        finalArrary.push(simulatedData[i].flat());
      }
    }
    return finalArrary;
  }

  getTargetAndInfluencers() {
    if (this.defectValue === undefined) {
      this.defectValue = this.defectPercentChange;
    }
    if (this.certainityValue === undefined) {
      this.certainityValue = this.certainityOldValue;
    }
    if (this.effortValue === undefined) {
      this.effortValue = this.effortPercentChange;
    }
    if (this.teamSizeValue === undefined) {
      this.teamSizeValue = this.teamSizePercentChange;
    }
    if (this.scheduleValue === undefined) {
      this.scheduleValue = this.schedulePercentChange;
    }
  }

  postChangedTargetInfluencerVal() {
    this.loader.start();
    const payload = ({
      'TemplateID': this.selectedTemplateVersion,
      'SimulationID': this.selectedSimulationVersion,
      'inputs': {
        'TargetCertainty': Number(this.certainityValue),
        'TargetVariable': Number(this.defectValue),
        'Influencers': {
          'Effort (Hrs)': Number(this.effortValue),
          'Team Size': Number(this.teamSizeValue),
          'Schedule (Days)': Number(this.scheduleValue)
        },
        'ChangedField': this.changeFieldName
      }
    });

    // this.setOutputData();
    if (this.changeFieldName === 'TargetCertainty') {
      const formControl = this.templateDataForm.controls;
      formControl['defects'].enable();
      formControl['efforts'].enable();
      formControl['teamSize'].enable();
      formControl['schedule'].enable();
    } else {
      this.makeInputControlsDisable();
    }
    this.ingrainApiService.post('WhatIfAnalysis', payload).subscribe(response => {
      if (response) {
        if (response.Status === 'C') {
          this.loader.stop();
          this.populateTemplateData(response);
          // this.graphCertaintyValues = response.CertaintyValues;
          this.targetCertanityValue = response.TargetCertainty;
          this.certainityValue = response.TargetCertainty;
          this.defectValue = response.TargetVariable;
          this.observationText = response.Observation;
          this.defectChartDataFormation(this.mean, this.stdDeviation, this.defectHistogramArray);

          this.setOutputData();

          this._alertService.success('What if analysis done successfully');
        } else if (response.Status === 'E') {
          this.loader.stop();
          this._alertService.error(response.ErrorMessage);
        }
      }
      this.loader.stop();
    }, error => {
      this.loader.stop();
      this._alertService.error('something went wrong in WhatIfAnalysis');
    });
  }

  setOutputData() {
    this.getTargetAndInfluencers();
    const payloadData = {
      'inputs': {
        'Influencers': {
          'Effort (Hrs)': this.whatIfEffortVal,
          'Team Size': this.whatIfTeamSizeVal,
          'Schedule (Days)': this.whatIfScheduleVal
        },
        'IncrementFlags': {
          'TargetVariable': this.defectIncrementalFlag,
          'Effort (Hrs)': this.effortIncrementalFlag,
          'Schedule (Days)': this.scheduleIncrementalFlag,
          'Team Size': this.teamSizeIncrementalFlag,
        },
        'PercentChange': {
          'TargetVariable': this.defectPercentChange,
          'Effort (Hrs)': this.effortPercentChange,
          'Schedule (Days)': this.schedulePercentChange,
          'Team Size': this.teamSizePercentChange
        },
        'TargetCertainty': this.certainityValue,
        'TargetVariable': this.whatIfDefectVal
      },
      'Observation': this.observationText,
      'SelectedCurrentRelease': this.currentReleaseName
    };
    this.saveOutputDataRequest = payloadData;
    this._simulationHeader.setOutputSavePayload(payloadData);
  }


  findTheConfidenceInterval(histoArray) {
    let xValue = 0; let xMaxValue = 0;
    const percentage = this.targetCertanityValue / 100;
    const xMaxArrayGroup = Math.max.apply(Math, histoArray.map(function (d) { return d.XAxisValues; }));
    const xMinArrayGroup = Math.min.apply(Math, histoArray.map(function (d) { return d.XAxisValues; }));
    if (Math.sign(xMinArrayGroup) === -1) {
      xValue = (xMaxArrayGroup + (Math.abs(xMinArrayGroup))) * percentage;
      xMaxValue = xValue - (Math.abs(xMinArrayGroup));
    } else {
      // xValue = (xMaxArrayGroup - xMinArrayGroup) * percentage;
      // xMaxValue = xValue + xMinArrayGroup;
      xValue = (xMaxArrayGroup - (Math.abs(xMinArrayGroup))) * percentage;
      xMaxValue = xValue + Math.abs(xMinArrayGroup);
    }
    return xMaxValue;
  }

  /**
   * Empty SVG element to redraw chart after getting new response from API
   * @param element
   */
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

  makeInputControlsDisable() {
    const formControl = this.templateDataForm.controls;
    if (this.changeFieldName === 'Defect') {
      formControl['efforts'].disable();
      formControl['teamSize'].disable();
      formControl['schedule'].disable();
      formControl['defects'].enable();
    } else if (this.changeFieldName === 'Effort (Hrs)' || this.changeFieldName === 'Team Size'
      || this.changeFieldName === 'Schedule (Days)') {
      formControl['defects'].disable();
      formControl['efforts'].enable();
      formControl['teamSize'].enable();
      formControl['schedule'].enable();
    }
  }

  barClicked(event) {
    this.horizontalChart = event;
    if (event.includes('Effort')) {
      event = 'Effort';
    } else if (event.includes('Team Size')) {
      event = 'TeamSize';
    } else {
      event = 'Schedule';
    }
    this.dataForHorizontalBar = _(this.simulationResult[event].Sensitivity_Analysis).toPairs()
      .orderBy([1], ['desc'])
      .fromPairs()
      .value();
  }

}
