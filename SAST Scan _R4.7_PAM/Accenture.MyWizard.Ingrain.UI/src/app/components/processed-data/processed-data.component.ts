import { Component, OnInit, OnDestroy } from '@angular/core';
import { ProcessedDataService } from '../../_services/processed-data.service';
import { timer, Subscription } from 'rxjs';


@Component({
  selector: 'app-processed-data',
  templateUrl: './processed-data.component.html',
  styleUrls: ['./processed-data.component.css']
})
export class ProcessedDataComponent implements OnInit, OnDestroy {

  processingTime: number;
  contacts = [];
  refresh: number;
  processedDataSubscription: Subscription;
  timerSubscription: Subscription;
  invokedPython: boolean;
  loading: boolean;

  constructor(private _processedDataService: ProcessedDataService) {
    this.invokedPython = true;
    this.loading = true;
  }

  ngOnInit() {
    this.refreshData();
  }

  subscribeToData(): void {
    this.timerSubscription = timer(1000).subscribe(() => this.refreshData());
  }

  refreshData(): void {
    this.refresh++;
    this.processedDataSubscription = this._processedDataService.getStatus().subscribe((data: string) => {
      const parsedData = JSON.parse(data);
      if (parsedData.useCaseDetails.Progress === 100) {
        this.invokedPython = false;
        this.loading = false;
        this.contacts = parsedData.useCaseDetails;
        this.processedDataSubscription.unsubscribe();
        if (this.timerSubscription) {
          this.timerSubscription.unsubscribe();
        }
      } else {
        this.subscribeToData();
      }
    });
  }

  ngOnDestroy(): void {
    if (this.timerSubscription) {
      this.timerSubscription.unsubscribe();
    }
    if (this.processedDataSubscription) {
      this.processedDataSubscription.unsubscribe();
    }
  }
}
