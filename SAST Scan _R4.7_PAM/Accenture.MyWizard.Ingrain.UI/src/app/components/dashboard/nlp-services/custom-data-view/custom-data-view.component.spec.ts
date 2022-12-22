import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CustomDataViewComponent } from './custom-data-view.component';

describe('DataViewComponent', () => {
  let component: CustomDataViewComponent;
  let fixture: ComponentFixture<CustomDataViewComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ CustomDataViewComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(CustomDataViewComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
